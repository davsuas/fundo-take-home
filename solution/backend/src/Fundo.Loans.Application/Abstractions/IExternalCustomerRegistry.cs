using Fundo.Loans.Application.Contracts;

namespace Fundo.Loans.Application.Abstractions;

/// <summary>
/// Port for the external system that mirrors our customers. One idempotent upsert: the same
/// call creates a new customer or updates a returning one. Implemented over HTTP in
/// Infrastructure; swap the adapter to change transport without touching the dispatcher's callers.
/// </summary>
public interface IExternalCustomerRegistry
{
    Task UpsertAsync(CustomerUpsertPayload payload, string idempotencyKey, CancellationToken cancellationToken);
}
