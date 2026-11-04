using BillingService.Application.Common;
using BillingService.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrcamentosController : ControllerBase
{
    private readonly ConsultarOrcamentoUseCase _consultarOrcamento;
    private readonly CriarOrcamentoUseCase _criarOrcamento;

    public OrcamentosController(ConsultarOrcamentoUseCase consultarOrcamento, CriarOrcamentoUseCase criarOrcamento)
    {
        _consultarOrcamento = consultarOrcamento;
        _criarOrcamento = criarOrcamento;
    }

    // GET: api/Orcamentos/ordem-servico/{ordemServicoId}
    // Consulta o link de pagamento de uma OS (gerado assincronamente ao
    // consumir o evento OrcamentoEnviadoEvent do OS Service).
    [HttpGet("ordem-servico/{ordemServicoId}")]
    public async Task<IActionResult> GetPorOrdemServico(Guid ordemServicoId)
    {
        var resultado = await _consultarOrcamento.ExecutarPorOrdemServicoAsync(ordemServicoId);
        return TraduzirResultado(resultado);
    }

    // POST: api/Orcamentos/simular
    // Endpoint de apoio para testes manuais/Postman sem depender da fila SQS —
    // dispara o mesmo fluxo que o consumidor de eventos dispararia.
    [HttpPost("simular")]
    [AllowAnonymous]
    public async Task<IActionResult> Simular([FromBody] SimularOrcamentoInput input)
    {
        var resultado = await _criarOrcamento.ExecutarAsync(input.OrdemServicoId, input.NumeroOS, input.ValorTotal);
        return TraduzirResultado(resultado);
    }

    private IActionResult TraduzirResultado<T>(OperationResult<T> resultado)
    {
        if (resultado.Sucesso)
            return Ok(resultado.Dados);

        return resultado.Erro switch
        {
            TipoErro.NaoEncontrado => NotFound(resultado.Mensagem),
            _ => BadRequest(resultado.Mensagem)
        };
    }

    public class SimularOrcamentoInput
    {
        public Guid OrdemServicoId { get; set; }
        public string NumeroOS { get; set; } = "";
        public decimal ValorTotal { get; set; }
    }
}
