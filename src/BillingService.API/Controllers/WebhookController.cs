using BillingService.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BillingService.API.Controllers;

/// <summary>
/// Recebe as notificações (IPN/webhook) do Mercado Pago quando o status de
/// um pagamento muda. A validação de autenticidade é feita por um segredo
/// compartilhado na query string (mesmo padrão simplificado do webhook de
/// e-mail da Fase 2/3) — numa evolução futura, troca-se pela validação de
/// assinatura oficial do Mercado Pago (x-signature).
/// </summary>
[ApiController]
[Route("webhook/mercadopago")]
[AllowAnonymous]
public class WebhookController : ControllerBase
{
    private readonly RegistrarPagamentoUseCase _registrarPagamento;
    private readonly IConfiguration _config;

    public WebhookController(RegistrarPagamentoUseCase registrarPagamento, IConfiguration config)
    {
        _registrarPagamento = registrarPagamento;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Receber(
        [FromBody] MercadoPagoNotificationInput input,
        [FromQuery(Name = "secret")] string? secret)
    {
        var secretEsperado = _config["MercadoPago:WebhookSecret"];
        if (!string.IsNullOrEmpty(secretEsperado) && secret != secretEsperado)
            return Unauthorized("Webhook secret inválido.");

        var resultado = await _registrarPagamento.ExecutarAsync(
            input.PreferenceId, input.PaymentId, input.Aprovado, input.MotivoRejeicao);

        return resultado.Sucesso ? Ok() : NotFound(resultado.Mensagem);
    }

    public class MercadoPagoNotificationInput
    {
        public string PreferenceId { get; set; } = "";
        public string PaymentId { get; set; } = "";
        public bool Aprovado { get; set; }
        public string? MotivoRejeicao { get; set; }
    }
}
