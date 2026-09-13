namespace Fundo.Loans.Domain.ValueObjects;

/// <summary>
/// Social Security Number value object. Holds the raw 9 digits only transiently in memory
/// (never persisted, logged, or serialized unmasked) so it can be hashed by the infrastructure
/// layer and compared against the blacklist / existing customers. Only rejects the wrong digit
/// count and the literal "000000000" — repeated-digit patterns like 111-11-1111 are otherwise
/// valid SSNs at this level; they are rejected later, by the blacklist rule.
/// </summary>
public sealed record Ssn
{
    public string Digits { get; }

    private Ssn(string digits)
    {
        Digits = digits;
    }

    public string Last4 => Digits[^4..];

    public static Ssn Create(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var digits = new string(raw.Where(char.IsDigit).ToArray());

        if (digits.Length != 9)
        {
            throw new DomainException("SSN must contain exactly 9 digits.");
        }

        if (digits == "000000000")
        {
            throw new DomainException("SSN cannot be all zeros.");
        }

        return new Ssn(digits);
    }

    public override string ToString() => $"***-**-{Last4}";
}
