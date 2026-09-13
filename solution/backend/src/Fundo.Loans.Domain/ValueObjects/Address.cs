namespace Fundo.Loans.Domain.ValueObjects;

public sealed record Address
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }

    private Address(string street, string city, string state, string postalCode)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
    }

    public static Address Create(string street, string city, string state, string postalCode)
    {
        ArgumentNullException.ThrowIfNull(street);
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(postalCode);

        var trimmedStreet = street.Trim();
        var trimmedCity = city.Trim();
        var normalizedState = state.Trim().ToUpperInvariant();
        var trimmedPostalCode = postalCode.Trim();

        if (trimmedStreet.Length is 0 or > 200)
        {
            throw new DomainException("Street must be between 1 and 200 characters.");
        }

        if (trimmedCity.Length is 0 or > 100)
        {
            throw new DomainException("City must be between 1 and 100 characters.");
        }

        if (normalizedState.Length != 2 || !normalizedState.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException("State must be a 2-letter uppercase code.");
        }

        if (trimmedPostalCode.Length != 5 || !trimmedPostalCode.All(char.IsAsciiDigit))
        {
            throw new DomainException("Postal code must be exactly 5 digits.");
        }

        return new Address(trimmedStreet, trimmedCity, normalizedState, trimmedPostalCode);
    }
}
