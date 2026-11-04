using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BillingService.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingService.Infrastructure.Payments;

/// <summary>
/// Integração com o Mercado Pago (ambiente sandbox) via API de Preferences
/// de Checkout Pro — https://www.mercadopago.com.br/developers. Cria uma
/// preferência de pagamento para o valor do orçamento e retorna o link de
/// checkout (init_point) a ser enviado ao cliente.
///
/// Requer MercadoPago:AccessToken (Access Token de teste/sandbox da conta
/// Mercado Pago do projeto) configurado via Secret do Kubernetes — nunca
/// commitado.
/// </summary>
public class MercadoPagoGateway : IMercadoPagoGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoPagoGateway> _logger;
    private readonly string? _accessToken;
    private readonly string _notificationUrl;

    public MercadoPagoGateway(HttpClient httpClient, IConfiguration config, ILogger<MercadoPagoGateway> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.mercadopago.com/");
        _logger = logger;
        _accessToken = config["MercadoPago:AccessToken"];
        _notificationUrl = config["MercadoPago:NotificationUrl"] ?? "";
    }

    public async Task<PreferenciaPagamento> CriarPreferenciaAsync(string numeroOS, decimal valorTotal, Guid ordemServicoId)
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            // Sem credenciais configuradas (ex.: ambiente de desenvolvimento
            // local) — devolve uma preferência simulada, permitindo exercitar
            // o restante do fluxo sem depender da rede/sandbox do Mercado Pago.
            _logger.LogWarning("MercadoPago:AccessToken não configurado — gerando preferência simulada para a OS {NumeroOS}", numeroOS);
            var preferenceIdSimulada = $"SIMULADO-{ordemServicoId}";
            return new PreferenciaPagamento(preferenceIdSimulada, $"https://sandbox.mercadopago.com.br/simulado/{preferenceIdSimulada}");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var body = new
        {
            items = new[]
            {
                new
                {
                    title = $"Oficina Mecânica Wagyu - OS {numeroOS}",
                    quantity = 1,
                    currency_id = "BRL",
                    unit_price = valorTotal
                }
            },
            external_reference = ordemServicoId.ToString(),
            notification_url = _notificationUrl
        };

        var response = await _httpClient.PostAsJsonAsync("checkout/preferences", body);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var json = await JsonDocument.ParseAsync(stream);

        var preferenceId = json.RootElement.GetProperty("id").GetString()!;
        var initPoint = json.RootElement.GetProperty("init_point").GetString()!;

        return new PreferenciaPagamento(preferenceId, initPoint);
    }
}
