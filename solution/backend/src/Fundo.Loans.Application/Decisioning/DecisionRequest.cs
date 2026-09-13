using Fundo.Loans.Domain.ValueObjects;

namespace Fundo.Loans.Application.Decisioning;

public sealed record DecisionRequest(PersonName Name, Address Address, string CompanyName, Ssn Ssn, Money RequestedAmount);
