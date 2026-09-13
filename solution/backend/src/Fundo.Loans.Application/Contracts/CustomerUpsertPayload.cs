namespace Fundo.Loans.Application.Contracts;

/// <summary>
/// The wire contract sent to the external service (PUT /api/v1/customers/{id}) and stored verbatim
/// as the outbox payload. One shape for create and update; <c>OccurredAtUtc</c> lets the receiver
/// ignore a retried older event that arrives after a newer one.
/// </summary>
public sealed record CustomerUpsertPayload(
    CustomerPayload Customer,
    LoanApplicationPayload Application,
    string Operation,
    DateTime OccurredAtUtc);

public sealed record CustomerPayload(
    Guid Id,
    string FirstName,
    string LastName,
    string CompanyName,
    string SsnLast4,
    AddressPayload Address);

public sealed record AddressPayload(string Street, string City, string State, string PostalCode);

public sealed record LoanApplicationPayload(Guid Id, decimal RequestedAmount, string Currency, string Status);
