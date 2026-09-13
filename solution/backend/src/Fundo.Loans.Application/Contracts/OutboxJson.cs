using System.Text.Json;

namespace Fundo.Loans.Application.Contracts;

/// <summary>JSON options for outbox payloads — the handler encodes and the dispatcher decodes with the same settings.</summary>
public static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
