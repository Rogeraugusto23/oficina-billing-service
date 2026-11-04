using BillingService.Application.Common;

namespace BillingService.Tests.Fakes;

public class FakeMercadoPagoGateway : IMercadoPagoGateway
{
    public int ChamadasRealizadas { get; private set; }

    public Task<PreferenciaPagamento> CriarPreferenciaAsync(string numeroOS, decimal valorTotal, Guid ordemServicoId)
    {
        ChamadasRealizadas++;
        var preferenceId = $"TESTE-{ordemServicoId}";
        return Task.FromResult(new PreferenciaPagamento(preferenceId, $"https://sandbox.mercadopago.com.br/{preferenceId}"));
    }
}
