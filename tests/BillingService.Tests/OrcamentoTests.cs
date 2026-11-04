using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using FluentAssertions;

namespace BillingService.Tests;

public class OrcamentoTests
{
    [Fact]
    public void CriarOrcamento_DeveIniciarComoPendentePagamento()
    {
        var orcamento = new Orcamento(Guid.NewGuid(), "202611031200", 150.50m);

        orcamento.Status.Should().Be(StatusPagamento.PendentePagamento);
        orcamento.ValorTotal.Should().Be(150.50m);
    }

    [Fact]
    public void CriarOrcamento_ComValorNegativo_DeveFalhar()
    {
        var act = () => new Orcamento(Guid.NewGuid(), "202611031200", -10m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ConfirmarPagamento_QuandoPendente_DeveMudarParaPago()
    {
        var orcamento = new Orcamento(Guid.NewGuid(), "202611031200", 100m);

        orcamento.ConfirmarPagamento("MP-123");

        orcamento.Status.Should().Be(StatusPagamento.Pago);
        orcamento.MercadoPagoPaymentId.Should().Be("MP-123");
    }

    [Fact]
    public void ConfirmarPagamento_QuandoJaPago_DeveFalhar()
    {
        var orcamento = new Orcamento(Guid.NewGuid(), "202611031200", 100m);
        orcamento.ConfirmarPagamento("MP-123");

        var act = () => orcamento.ConfirmarPagamento("MP-456");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancelar_DeveSerIdempotente()
    {
        var orcamento = new Orcamento(Guid.NewGuid(), "202611031200", 100m);

        orcamento.Cancelar();
        orcamento.Cancelar();

        orcamento.Status.Should().Be(StatusPagamento.Cancelado);
    }

    [Fact]
    public void Reidratar_DevePreservarIdOriginal()
    {
        var idOriginal = Guid.NewGuid();
        var ordemServicoId = Guid.NewGuid();

        var orcamento = Orcamento.Reidratar(
            idOriginal, ordemServicoId, "202611031200", 200m, StatusPagamento.Pago,
            "PREF-1", "PAY-1", "https://link", DateTime.UtcNow, DateTime.UtcNow);

        orcamento.Id.Should().Be(idOriginal);
        orcamento.OrdemServicoId.Should().Be(ordemServicoId);
        orcamento.Status.Should().Be(StatusPagamento.Pago);
    }
}
