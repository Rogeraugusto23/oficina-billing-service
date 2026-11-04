using BillingService.Application.Common;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;

namespace BillingService.Application.UseCases;

public class ConsultarOrcamentoUseCase
{
    private readonly IOrcamentoRepository _repository;

    public ConsultarOrcamentoUseCase(IOrcamentoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<Orcamento>> ExecutarPorOrdemServicoAsync(Guid ordemServicoId)
    {
        var orcamento = await _repository.ObterPorOrdemServicoIdAsync(ordemServicoId);
        if (orcamento == null)
            return OperationResult<Orcamento>.Falha(TipoErro.NaoEncontrado, "Orçamento não encontrado para esta OS.");

        return OperationResult<Orcamento>.Ok(orcamento);
    }
}
