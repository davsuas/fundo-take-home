namespace Fundo.Loans.Api.Contracts;

public sealed record SubmitLoanApplicationRequest(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string PostalCode,
    string CompanyName,
    decimal RequestedAmount,
    string Ssn);

/// <summary><c>Outcome</c> is "Approved" or "Denied"; invalid input is answered with a 400 ValidationProblem instead.</summary>
public sealed record SubmitLoanApplicationResponse(
    string Outcome,
    Guid? ApplicationId,
    Guid? CustomerId,
    bool IsReturningCustomer,
    string? RuleCode,
    string? Reason);
