namespace BillingService.Application.Common;

public record PreferenciaPagamento(string PreferenceId, string LinkPagamento);

/// <summary>
/// Abstração do gateway de pagamento (Mercado Pago, ambiente sandbox). A
/// Application depende apenas deste contrato — a implementação real via
/// HTTP fica em Infrastructure/Payments, e os testes usam um dublê.
/// </summary>
public interface IMercadoPagoGateway
{
    Task<PreferenciaPagamento> CriarPreferenciaAsync(string numeroOS, decimal valorTotal, Guid ordemServicoId);
}
