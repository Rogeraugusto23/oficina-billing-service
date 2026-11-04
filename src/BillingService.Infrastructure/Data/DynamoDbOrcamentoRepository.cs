using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BillingService.Infrastructure.Data;

/// <summary>
/// Persistência em DynamoDB (NoSQL) — banco próprio do Billing Service,
/// conforme exigido pela Fase 4 ("cada serviço com seu próprio banco, sem
/// acesso direto ao banco de outro serviço"). Tabela de item único (chave de
/// partição "Id") com um GSI implícito simulado por scan em
/// ObterPorOrdemServicoIdAsync — numa evolução futura isso vira um índice
/// secundário global real (OrdemServicoId-index), citado no README.
/// </summary>
public class DynamoDbOrcamentoRepository : IOrcamentoRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    public DynamoDbOrcamentoRepository(IAmazonDynamoDB client, IConfiguration config)
    {
        _client = client;
        _tableName = config["DynamoDb:OrcamentosTableName"] ?? "oficina-wagyu-orcamentos";
    }

    public async Task<Orcamento?> ObterPorIdAsync(Guid id)
    {
        var table = Table.LoadTable(_client, _tableName);
        var doc = await table.GetItemAsync(id.ToString());
        return doc == null ? null : MapToEntity(doc);
    }

    public async Task<Orcamento?> ObterPorOrdemServicoIdAsync(Guid ordemServicoId)
    {
        var table = Table.LoadTable(_client, _tableName);
        var search = table.Scan(new ScanFilter().AddCondition("OrdemServicoId", ScanOperator.Equal, ordemServicoId.ToString()));
        var page = await search.GetNextSetAsync();
        return page.Count > 0 ? MapToEntity(page[0]) : null;
    }

    public async Task<Orcamento?> ObterPorMercadoPagoPreferenceIdAsync(string preferenceId)
    {
        var table = Table.LoadTable(_client, _tableName);
        var search = table.Scan(new ScanFilter().AddCondition("MercadoPagoPreferenceId", ScanOperator.Equal, preferenceId));
        var page = await search.GetNextSetAsync();
        return page.Count > 0 ? MapToEntity(page[0]) : null;
    }

    public async Task SalvarAsync(Orcamento orcamento)
    {
        var table = Table.LoadTable(_client, _tableName);
        var doc = MapToDocument(orcamento);
        await table.PutItemAsync(doc);
    }

    private static Document MapToDocument(Orcamento o)
    {
        var doc = new Document
        {
            ["Id"] = o.Id.ToString(),
            ["OrdemServicoId"] = o.OrdemServicoId.ToString(),
            ["NumeroOS"] = o.NumeroOS,
            ["ValorTotal"] = o.ValorTotal,
            ["Status"] = o.Status.ToString(),
            ["CriadoEm"] = o.CriadoEm.ToString("O")
        };
        if (o.MercadoPagoPreferenceId != null) doc["MercadoPagoPreferenceId"] = o.MercadoPagoPreferenceId;
        if (o.MercadoPagoPaymentId != null) doc["MercadoPagoPaymentId"] = o.MercadoPagoPaymentId;
        if (o.LinkPagamento != null) doc["LinkPagamento"] = o.LinkPagamento;
        if (o.AtualizadoEm != null) doc["AtualizadoEm"] = o.AtualizadoEm.Value.ToString("O");
        return doc;
    }

    private static Orcamento MapToEntity(Document doc)
    {
        return Orcamento.Reidratar(
            id: Guid.Parse(doc["Id"].AsString()),
            ordemServicoId: Guid.Parse(doc["OrdemServicoId"].AsString()),
            numeroOS: doc["NumeroOS"].AsString(),
            valorTotal: doc["ValorTotal"].AsDecimal(),
            status: Enum.Parse<StatusPagamento>(doc["Status"].AsString()),
            mercadoPagoPreferenceId: doc.TryGetValue("MercadoPagoPreferenceId", out var preferenceId) ? preferenceId.AsString() : null,
            mercadoPagoPaymentId: doc.TryGetValue("MercadoPagoPaymentId", out var paymentId) ? paymentId.AsString() : null,
            linkPagamento: doc.TryGetValue("LinkPagamento", out var link) ? link.AsString() : null,
            criadoEm: DateTime.Parse(doc["CriadoEm"].AsString(), null, System.Globalization.DateTimeStyles.RoundtripKind),
            atualizadoEm: doc.TryGetValue("AtualizadoEm", out var atualizadoEm)
                ? DateTime.Parse(atualizadoEm.AsString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
                : null);
    }
}
