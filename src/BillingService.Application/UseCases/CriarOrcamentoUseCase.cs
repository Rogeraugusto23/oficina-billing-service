using BillingService.Application.Common;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;

namespace BillingService.Application.UseCases;

/// <summary>
/// Disparado ao consumir OrcamentoEnviadoEvent (publicado pelo OS Service
/// quando o diagnóstico é concluído e o orçamento é enviado ao cliente).
/// Cria o registro de cobrança no Billing Service e gera o link de pagamento
/// Mercado Pago (sandbox) — sem nunca tocar no banco do OS Service.
/// </summary>
public class CriarOrcamentoUseCase
{
    private readonly IOrcamentoRepository _repository;
    private readonly IMercadoPagoGateway _mercadoPago;

    public CriarOrcamentoUseCase(IOrcamentoRepository repository, IMercadoPagoGateway mercadoPago)
    {
        _repository = repository;
        _mercadoPago = mercadoPago;
    }

    public async Task<OperationResult<Orcamento>> ExecutarAsync(Guid ordemServicoId, string numeroOS, decimal valorTotal)
    {
        var existente = await _repository.ObterPorOrdemServicoIdAsync(ordemServicoId);
        if (existente != null)
            return OperationResult<Orcamento>.Ok(existente, "Orçamento já existia para esta OS.");

        var orcamento = new Orcamento(ordemServicoId, numeroOS, valorTotal);

        var preferencia = await _mercadoPago.CriarPreferenciaAsync(numeroOS, valorTotal, ordemServicoId);
        orcamento.DefinirPreferenciaDePagamento(preferencia.PreferenceId, preferencia.LinkPagamento);

        await _repository.SalvarAsync(orcamento);

        return OperationResult<Orcamento>.Ok(orcamento, "Orçamento criado e link de pagamento gerado.");
    }
}
