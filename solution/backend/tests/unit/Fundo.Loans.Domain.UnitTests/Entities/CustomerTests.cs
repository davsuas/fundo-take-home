using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Domain.ValueObjects;
using Xunit;

namespace Fundo.Loans.Domain.UnitTests.Entities;

public class CustomerTests
{
    private static readonly PersonName Name = PersonName.Create("Jane", "Doe");
    private static readonly Address Address = Address.Create("1 Main St", "Springfield", "CA", "94105");

    [Fact]
    public void Register_AssignsIdAndTimestamps()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var customer = Customer.Register(Name, Address, "Acme", "hash", "5555", now);

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal(now, customer.CreatedAtUtc);
        Assert.Equal(now, customer.UpdatedAtUtc);
        Assert.Equal("Acme", customer.CompanyName);
    }

    [Fact]
    public void UpdateDetails_UpdatesFieldsAndBumpsUpdatedAt()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updated = created.AddDays(1);
        var customer = Customer.Register(Name, Address, "Acme", "hash", "5555", created);

        var newName = PersonName.Create("Janet", "Doe");
        var newAddress = Address.Create("2 Elm St", "Shelbyville", "CA", "94106");

        customer.UpdateDetails(newName, newAddress, "Acme Corp", updated);

        Assert.Equal(newName, customer.Name);
        Assert.Equal(newAddress, customer.Address);
        Assert.Equal("Acme Corp", customer.CompanyName);
        Assert.Equal(created, customer.CreatedAtUtc);
        Assert.Equal(updated, customer.UpdatedAtUtc);
    }
}
