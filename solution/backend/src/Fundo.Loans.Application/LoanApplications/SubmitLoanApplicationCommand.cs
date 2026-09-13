namespace Fundo.Loans.Application.LoanApplications;

public sealed record SubmitLoanApplicationCommand(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string PostalCode,
    string CompanyName,
    decimal RequestedAmount,
    string Ssn);
