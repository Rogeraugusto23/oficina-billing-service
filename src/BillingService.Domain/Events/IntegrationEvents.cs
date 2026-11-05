namespace BillingService.Domain.Events;

/// <summary>
/// Eventos de integração do Billing Service. Os eventos "consumidos" abaixo
/// têm exatamente o mesmo formato publicado pelo OS Service (contrato de
/// mensageria compartilhado entre os dois repositórios) — o Billing Service
/// não referencia o código do OS Service, apenas concorda com o shape JSON.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
}

// ---------- Consumidos (publicados pelo OS Service) ----------

public sealed record OrdemServicoAbertaEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public Guid ClienteId { get; init; }
    public Guid VeiculoId { get; init; }
}

public sealed record OrcamentoEnviadoEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public decimal ValorTotal { get; init; }
}

public sealed record OrcamentoRejeitadoEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
}

// ---------- Publicados pelo Billing Service ----------

public sealed record PagamentoAprovadoEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public string MercadoPagoPaymentId { get; init; } = "";
}

public sealed record PagamentoRejeitadoEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public string? Motivo { get; init; }
}

/// <summary>Consumido: OS cancelada pelo OS Service (ex.: falha na execução) → estorno/cancelamento da cobrança.</summary>
public sealed record OrdemServicoCanceladaEvent : IntegrationEvent
{
    public Guid OrdemServicoId { get; init; }
    public string NumeroOS { get; init; } = "";
    public string Motivo { get; init; } = "";
}
