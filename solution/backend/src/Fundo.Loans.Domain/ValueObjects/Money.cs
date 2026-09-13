namespace Fundo.Loans.Domain.ValueObjects;

/// <summary>Requested loan amount. USD only — multi-currency is out of scope for this brief.</summary>
public sealed record Money
{
    public const string Currency = "USD";

    public decimal Amount { get; }

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money Create(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Requested amount must be greater than zero.");
        }

        if (amount > 1_000_000)
        {
            throw new DomainException("Requested amount must not exceed 1,000,000.");
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven));
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
