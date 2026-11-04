using BillingService.Application.UseCases;
using BillingService.Domain.Entities;
using BillingService.Domain.Events;
using BillingService.Tests.Fakes;
using FluentAssertions;

namespace BillingService.Tests;

public class UseCasesTests
{
    [Fact]
    public async Task CriarOrcamento_DeveGerarLinkDePagamento_EPersistir()
    {
        var repository = new FakeOrcamentoRepository();
        var gateway = new FakeMercadoPagoGateway();
        var useCase = new CriarOrcamentoUseCase(repository, gateway);

        var ordemServicoId = Guid.NewGuid();
        var resultado = await useCase.ExecutarAsync(ordemServicoId, "202611031200", 300m);

        resultado.Sucesso.Should().BeTrue();
        resultado.Dados!.LinkPagamento.Should().NotBeNullOrEmpty();
        gateway.ChamadasRealizadas.Should().Be(1);

        var persistido = await repository.ObterPorOrdemServicoIdAsync(ordemServicoId);
        persistido.Should().NotBeNull();
    }

    [Fact]
    public async Task CriarOrcamento_Chamado2Vezes_NaoDeveDuplicar()
    {
        var repository = new FakeOrcamentoRepository();
        var gateway = new FakeMercadoPagoGateway();
        var useCase = new CriarOrcamentoUseCase(repository, gateway);
        var ordemServicoId = Guid.NewGuid();

        await useCase.ExecutarAsync(ordemServicoId, "202611031200", 300m);
        await useCase.ExecutarAsync(ordemServicoId, "202611031200", 300m);

        gateway.ChamadasRealizadas.Should().Be(1, "a segunda chamada deve ser idempotente e não gerar uma nova preferência");
    }

    [Fact]
    public async Task RegistrarPagamento_Aprovado_DevePublicarPagamentoAprovadoEvent()
    {
        var repository = new FakeOrcamentoRepository();
        var publisher = new FakeEventPublisher();

        var orcamento = new Orcamento(Guid.NewGuid(), "202611031200", 300m);
        orcamento.DefinirPreferenciaDePagamento("PREF-1", "https://link");
        await repository.SalvarAsync(orcamento);

        var useCase = new RegistrarPagamentoUseCase(repository, publisher);
        var resultado = await useCase.ExecutarAsync("PREF-1", "PAY-1", aprovado: true);

        resultado.Sucesso.Should().BeTrue();
        publisher.EventosPublicados.Should().ContainSingle().Which.Should().BeOfType<PagamentoAprovadoEvent>();
    }

    [Fact]
    public async Task RegistrarPagamento_Rejeitado_DevePublicarPagamentoRejeitadoEvent()
    {
        var repository = new FakeOrcamentoRepository();
        var publisher = new FakeEventPublisher();

        var orcamento = new Orcamento(Guid.NewGuid(), "202611031200", 300m);
        orcamento.DefinirPreferenciaDePagamento("PREF-2", "https://link");
        await repository.SalvarAsync(orcamento);

        var useCase = new RegistrarPagamentoUseCase(repository, publisher);
        var resultado = await useCase.ExecutarAsync("PREF-2", "PAY-2", aprovado: false, motivoRejeicao: "Cartão recusado");

        resultado.Sucesso.Should().BeTrue();
        publisher.EventosPublicados.Should().ContainSingle().Which.Should().BeOfType<PagamentoRejeitadoEvent>();
    }

    [Fact]
    public async Task CancelarOrcamento_QuandoExiste_DeveCancelar()
    {
        var repository = new FakeOrcamentoRepository();
        var ordemServicoId = Guid.NewGuid();
        var orcamento = new Orcamento(ordemServicoId, "202611031200", 150m);
        await repository.SalvarAsync(orcamento);

        var useCase = new CancelarOrcamentoUseCase(repository);
        var resultado = await useCase.ExecutarAsync(ordemServicoId);

        resultado.Sucesso.Should().BeTrue();
        var atualizado = await repository.ObterPorOrdemServicoIdAsync(ordemServicoId);
        atualizado!.Status.Should().Be(BillingService.Domain.Enums.StatusPagamento.Cancelado);
    }
}
