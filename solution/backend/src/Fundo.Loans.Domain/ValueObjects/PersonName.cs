namespace Fundo.Loans.Domain.ValueObjects;

public sealed record PersonName
{
    public string First { get; }
    public string Last { get; }

    private PersonName(string first, string last)
    {
        First = first;
        Last = last;
    }

    public static PersonName Create(string first, string last)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(last);

        var trimmedFirst = first.Trim();
        var trimmedLast = last.Trim();

        if (trimmedFirst.Length is 0 or > 100)
        {
            throw new DomainException("First name must be between 1 and 100 characters.");
        }

        if (trimmedLast.Length is 0 or > 100)
        {
            throw new DomainException("Last name must be between 1 and 100 characters.");
        }

        return new PersonName(trimmedFirst, trimmedLast);
    }

    public override string ToString() => $"{First} {Last}";
}
