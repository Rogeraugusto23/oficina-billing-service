using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BillingService.Application.Common;
using BillingService.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingService.Infrastructure.Messaging;

public class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly ILogger<SnsEventPublisher> _logger;
    private readonly string? _topicArn;

    public SnsEventPublisher(IAmazonSimpleNotificationService sns, IConfiguration config, ILogger<SnsEventPublisher> logger)
    {
        _sns = sns;
        _logger = logger;
        _topicArn = config["Messaging:SnsTopicArn"];
    }

    public async Task PublicarAsync<TEvent>(TEvent evento) where TEvent : IntegrationEvent
    {
        var tipoEvento = typeof(TEvent).Name;
        var payload = JsonSerializer.Serialize(evento);

        if (string.IsNullOrWhiteSpace(_topicArn))
        {
            _logger.LogWarning("Messaging:SnsTopicArn não configurado — evento {EventType} não publicado: {Payload}", tipoEvento, payload);
            return;
        }

        try
        {
            await _sns.PublishAsync(new PublishRequest
            {
                TopicArn = _topicArn,
                Message = payload,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["EventType"] = new MessageAttributeValue { DataType = "String", StringValue = tipoEvento }
                }
            });
            _logger.LogInformation("Evento {EventType} publicado no SNS (CorrelationId={CorrelationId})", tipoEvento, evento.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar evento {EventType} (CorrelationId={CorrelationId})", tipoEvento, evento.CorrelationId);
        }
    }
}
