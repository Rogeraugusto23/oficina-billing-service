using BillingService.Application.Common;
using BillingService.Domain.Interfaces;

namespace BillingService.Application.UseCases;

/// <summary>
/// Compensação da Saga: disparado ao consumir OrcamentoRejeitadoEvent
/// (cliente rejeitou o orçamento no OS Service) para cancelar/estornar
/// qualquer cobrança pendente aqui no Billing Service.
/// </summary>
public class CancelarOrcamentoUseCase
{
    private readonly IOrcamentoRepository _repository;

    public CancelarOrcamentoUseCase(IOrcamentoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<bool>> ExecutarAsync(Guid ordemServicoId)
    {
        var orcamento = await _repository.ObterPorOrdemServicoIdAsync(ordemServicoId);
        if (orcamento == null)
            return OperationResult<bool>.Falha(TipoErro.NaoEncontrado, "Orçamento não encontrado para esta OS.");

        orcamento.Cancelar();
        await _repository.SalvarAsync(orcamento);

        return OperationResult<bool>.Ok(true, "Orçamento cancelado (compensação da Saga).");
    }
}
