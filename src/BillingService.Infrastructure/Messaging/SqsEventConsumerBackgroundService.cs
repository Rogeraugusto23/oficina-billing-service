using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using BillingService.Application.Common;
using BillingService.Application.UseCases;
using BillingService.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingService.Infrastructure.Messaging;

/// <summary>
/// Consome a fila SQS do Billing Service (subscrita ao tópico SNS de eventos
/// de integração, filtrada por EventType) e despacha para os Use Cases
/// correspondentes. Faz parte da Saga orquestrada pelo OS Service: este
/// serviço reage de forma assíncrona, nunca é chamado sincronamente pela OS.
///
/// Fluxo tratado aqui:
///   OrcamentoEnviadoEvent   -> CriarOrcamentoUseCase (gera cobrança + link MP)
///   OrcamentoRejeitadoEvent -> CancelarOrcamentoUseCase (compensação)
/// </summary>
public class SqsEventConsumerBackgroundService : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SqsEventConsumerBackgroundService> _logger;
    private readonly string? _queueUrl;

    public SqsEventConsumerBackgroundService(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<SqsEventConsumerBackgroundService> logger)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueUrl = config["Messaging:SqsQueueUrl"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_queueUrl))
        {
            _logger.LogWarning("Messaging:SqsQueueUrl não configurado — consumidor de eventos desligado.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 10,
                    MessageAttributeNames = new List<string> { "EventType" }
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await ProcessarMensagemAsync(message);
                    await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Encerramento normal do serviço.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consumir fila SQS de eventos de integração");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessarMensagemAsync(Message message)
    {
        if (!message.MessageAttributes.TryGetValue("EventType", out var eventTypeAttr))
        {
            _logger.LogWarning("Mensagem SQS sem atributo EventType, ignorada: {MessageId}", message.MessageId);
            return;
        }

        using var scope = _scopeFactory.CreateScope();

        switch (eventTypeAttr.StringValue)
        {
            case nameof(OrcamentoEnviadoEvent):
            {
                var evento = JsonSerializer.Deserialize<OrcamentoEnviadoEvent>(message.Body)!;
                var useCase = scope.ServiceProvider.GetRequiredService<CriarOrcamentoUseCase>();
                await useCase.ExecutarAsync(evento.OrdemServicoId, evento.NumeroOS, evento.ValorTotal);
                break;
            }
            case nameof(OrcamentoRejeitadoEvent):
            {
                var evento = JsonSerializer.Deserialize<OrcamentoRejeitadoEvent>(message.Body)!;
                var useCase = scope.ServiceProvider.GetRequiredService<CancelarOrcamentoUseCase>();
                await useCase.ExecutarAsync(evento.OrdemServicoId);
                break;
            }
            case nameof(OrdemServicoCanceladaEvent):
            {
                // Compensação da Saga: execução falhou, OS cancelada -> cancela/estorna a cobrança.
                var evento = JsonSerializer.Deserialize<OrdemServicoCanceladaEvent>(message.Body)!;
                var useCase = scope.ServiceProvider.GetRequiredService<CancelarOrcamentoUseCase>();
                await useCase.ExecutarAsync(evento.OrdemServicoId);
                break;
            }
            default:
                _logger.LogDebug("Evento {EventType} ignorado pelo Billing Service", eventTypeAttr.StringValue);
                break;
        }
    }
}
