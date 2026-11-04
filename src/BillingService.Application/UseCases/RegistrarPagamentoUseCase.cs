using BillingService.Application.Common;
using BillingService.Domain.Events;
using BillingService.Domain.Interfaces;

namespace BillingService.Application.UseCases;

/// <summary>
/// Acionado pelo webhook do Mercado Pago quando o status de um pagamento
/// muda. Publica PagamentoAprovadoEvent/PagamentoRejeitadoEvent para o OS
/// Service avançar (ou compensar) a OS — ver Dias 10-11 do roadmap para a
/// orquestração completa da Saga.
/// </summary>
public class RegistrarPagamentoUseCase
{
    private readonly IOrcamentoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public RegistrarPagamentoUseCase(IOrcamentoRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<OperationResult<bool>> ExecutarAsync(string preferenceId, string paymentId, bool aprovado, string? motivoRejeicao = null)
    {
        var orcamento = await _repository.ObterPorMercadoPagoPreferenceIdAsync(preferenceId);
        if (orcamento == null)
            return OperationResult<bool>.Falha(TipoErro.NaoEncontrado, $"Orçamento com preference {preferenceId} não encontrado.");

        if (aprovado)
        {
            orcamento.ConfirmarPagamento(paymentId);
            await _repository.SalvarAsync(orcamento);

            await _eventPublisher.PublicarAsync(new PagamentoAprovadoEvent
            {
                CorrelationId = orcamento.OrdemServicoId,
                OrdemServicoId = orcamento.OrdemServicoId,
                NumeroOS = orcamento.NumeroOS,
                MercadoPagoPaymentId = paymentId
            });
        }
        else
        {
            orcamento.RejeitarPagamento(motivoRejeicao);
            await _repository.SalvarAsync(orcamento);

            await _eventPublisher.PublicarAsync(new PagamentoRejeitadoEvent
            {
                CorrelationId = orcamento.OrdemServicoId,
                OrdemServicoId = orcamento.OrdemServicoId,
                NumeroOS = orcamento.NumeroOS,
                Motivo = motivoRejeicao
            });
        }

        return OperationResult<bool>.Ok(true);
    }
}
