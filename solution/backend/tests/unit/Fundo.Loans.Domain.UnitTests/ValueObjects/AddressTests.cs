using Fundo.Loans.Domain;
using Fundo.Loans.Domain.ValueObjects;
using Xunit;

namespace Fundo.Loans.Domain.UnitTests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Create_NormalizesStateToUppercase()
    {
        var address = Address.Create("1 Main St", "Springfield", "ca", "94105");

        Assert.Equal("CA", address.State);
    }

    [Theory]
    [InlineData("C")]
    [InlineData("CAL")]
    [InlineData("12")]
    public void Create_RejectsInvalidState(string state)
    {
        Assert.Throws<DomainException>(() => Address.Create("1 Main St", "Springfield", state, "94105"));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("abcde")]
    public void Create_RejectsInvalidPostalCode(string postalCode)
    {
        Assert.Throws<DomainException>(() => Address.Create("1 Main St", "Springfield", "CA", postalCode));
    }

    [Fact]
    public void Create_RejectsEmptyStreetOrCity()
    {
        Assert.Throws<DomainException>(() => Address.Create("", "Springfield", "CA", "94105"));
        Assert.Throws<DomainException>(() => Address.Create("1 Main St", "", "CA", "94105"));
    }
}
