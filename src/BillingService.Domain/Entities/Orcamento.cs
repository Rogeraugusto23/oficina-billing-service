using BillingService.Domain.Enums;

namespace BillingService.Domain.Entities;

/// <summary>
/// Agregado raiz do Billing Service — representa a cobrança (orçamento +
/// pagamento) de uma OrdemServico do OS Service. O Billing Service NUNCA lê
/// ou escreve no banco do OS Service: tudo que precisa (OrdemServicoId,
/// NumeroOS, ValorTotal) chega via evento assíncrono (OrdemServicoAbertaEvent
/// / OrcamentoEnviadoEvent) e é copiado para este agregado próprio,
/// persistido no DynamoDB.
/// </summary>
public class Orcamento
{
    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public string NumeroOS { get; private set; }
    public decimal ValorTotal { get; private set; }
    public StatusPagamento Status { get; private set; }
    public string? MercadoPagoPreferenceId { get; private set; }
    public string? MercadoPagoPaymentId { get; private set; }
    public string? LinkPagamento { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? AtualizadoEm { get; private set; }

    private Orcamento() { NumeroOS = ""; }

    public Orcamento(Guid ordemServicoId, string numeroOS, decimal valorTotal)
    {
        if (valorTotal < 0)
            throw new ArgumentException("Valor total não pode ser negativo.");

        Id = Guid.NewGuid();
        OrdemServicoId = ordemServicoId;
        NumeroOS = numeroOS;
        ValorTotal = valorTotal;
        Status = StatusPagamento.PendentePagamento;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Reidrata um Orcamento já existente a partir do que foi persistido no
    /// DynamoDB, preservando o Id original (o construtor público sempre gera
    /// um Id novo, por ser pensado para criação). Usado apenas pela camada de
    /// Infrastructure ao mapear um Document de volta para a entidade.
    /// </summary>
    public static Orcamento Reidratar(
        Guid id, Guid ordemServicoId, string numeroOS, decimal valorTotal, StatusPagamento status,
        string? mercadoPagoPreferenceId, string? mercadoPagoPaymentId, string? linkPagamento,
        DateTime criadoEm, DateTime? atualizadoEm)
    {
        return new Orcamento
        {
            Id = id,
            OrdemServicoId = ordemServicoId,
            NumeroOS = numeroOS,
            ValorTotal = valorTotal,
            Status = status,
            MercadoPagoPreferenceId = mercadoPagoPreferenceId,
            MercadoPagoPaymentId = mercadoPagoPaymentId,
            LinkPagamento = linkPagamento,
            CriadoEm = criadoEm,
            AtualizadoEm = atualizadoEm
        };
    }

    public void DefinirPreferenciaDePagamento(string preferenceId, string linkPagamento)
    {
        MercadoPagoPreferenceId = preferenceId;
        LinkPagamento = linkPagamento;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void ConfirmarPagamento(string paymentId)
    {
        if (Status != StatusPagamento.PendentePagamento)
            throw new InvalidOperationException($"Orçamento não está pendente de pagamento (status atual: {Status}).");

        Status = StatusPagamento.Pago;
        MercadoPagoPaymentId = paymentId;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RejeitarPagamento(string? motivo = null)
    {
        if (Status != StatusPagamento.PendentePagamento)
            throw new InvalidOperationException($"Orçamento não está pendente de pagamento (status atual: {Status}).");

        Status = StatusPagamento.Rejeitado;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Compensação da Saga: usada quando a OS é cancelada (orçamento rejeitado
    /// no OS Service) ou quando a execução falha depois do pagamento — estorna
    /// a cobrança pendente/efetuada.
    /// </summary>
    public void Cancelar()
    {
        if (Status == StatusPagamento.Cancelado)
            return;

        Status = StatusPagamento.Cancelado;
        AtualizadoEm = DateTime.UtcNow;
    }
}
