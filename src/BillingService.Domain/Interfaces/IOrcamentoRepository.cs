using BillingService.Domain.Entities;

namespace BillingService.Domain.Interfaces;

public interface IOrcamentoRepository
{
    Task<Orcamento?> ObterPorIdAsync(Guid id);
    Task<Orcamento?> ObterPorOrdemServicoIdAsync(Guid ordemServicoId);
    Task<Orcamento?> ObterPorMercadoPagoPreferenceIdAsync(string preferenceId);
    Task SalvarAsync(Orcamento orcamento);
}
