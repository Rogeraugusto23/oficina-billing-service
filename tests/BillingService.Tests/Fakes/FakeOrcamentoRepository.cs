using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;

namespace BillingService.Tests.Fakes;

public class FakeOrcamentoRepository : IOrcamentoRepository
{
    private readonly Dictionary<Guid, Orcamento> _porId = new();

    public Task<Orcamento?> ObterPorIdAsync(Guid id) =>
        Task.FromResult(_porId.GetValueOrDefault(id));

    public Task<Orcamento?> ObterPorOrdemServicoIdAsync(Guid ordemServicoId) =>
        Task.FromResult(_porId.Values.FirstOrDefault(o => o.OrdemServicoId == ordemServicoId));

    public Task<Orcamento?> ObterPorMercadoPagoPreferenceIdAsync(string preferenceId) =>
        Task.FromResult(_porId.Values.FirstOrDefault(o => o.MercadoPagoPreferenceId == preferenceId));

    public Task SalvarAsync(Orcamento orcamento)
    {
        _porId[orcamento.Id] = orcamento;
        return Task.CompletedTask;
    }
}
