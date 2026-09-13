using Fundo.Loans.Domain.ValueObjects;

namespace Fundo.Loans.Domain.Entities;

/// <summary>
/// A customer identified by the hash of their SSN. The plaintext SSN never reaches this entity —
/// only the HMAC hash (for lookup) and the last 4 digits (for display) are stored.
/// </summary>
public sealed class Customer
{
    public Guid Id { get; private set; }
    public PersonName Name { get; private set; }
    public Address Address { get; private set; }
    public string CompanyName { get; private set; }
    public string SsnHash { get; private set; }
    public string SsnLast4 { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Customer(
        Guid id,
        PersonName name,
        Address address,
        string companyName,
        string ssnHash,
        string ssnLast4,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Address = address;
        CompanyName = companyName;
        SsnHash = ssnHash;
        SsnLast4 = ssnLast4;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

#pragma warning disable CS8618 // EF Core materialization constructor.
    private Customer()
    {
    }
#pragma warning restore CS8618

    public static Customer Register(
        PersonName name,
        Address address,
        string companyName,
        string ssnHash,
        string ssnLast4,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ssnHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(ssnLast4);

        return new Customer(Guid.CreateVersion7(), name, address, companyName.Trim(), ssnHash, ssnLast4, nowUtc);
    }

    public void UpdateDetails(PersonName name, Address address, string companyName, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName);

        Name = name;
        Address = address;
        CompanyName = companyName.Trim();
        UpdatedAtUtc = nowUtc;
    }
}
