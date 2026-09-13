using Fundo.Loans.Domain;
using Fundo.Loans.Domain.ValueObjects;
using Xunit;

namespace Fundo.Loans.Domain.UnitTests.ValueObjects;

public class SsnTests
{
    [Theory]
    [InlineData("555-55-5555")]
    [InlineData("555555555")]
    [InlineData(" 555 55 5555 ")]
    public void Create_AcceptsNineDigitsInAnyCommonFormat(string raw)
    {
        var ssn = Ssn.Create(raw);

        Assert.Equal("555555555", ssn.Digits);
        Assert.Equal("5555", ssn.Last4);
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("1234567890")]
    [InlineData("")]
    public void Create_RejectsWrongLength(string raw)
    {
        Assert.Throws<DomainException>(() => Ssn.Create(raw));
    }

    [Fact]
    public void Create_RejectsAllZeros()
    {
        Assert.Throws<DomainException>(() => Ssn.Create("000000000"));
    }

    [Fact]
    public void Create_AllowsRepeatedDigitPatternsOtherThanAllZeros()
    {
        // 111-11-1111 etc. are valid SSNs at the value-object level; they are only rejected
        // later, by the blacklist rule, which is exactly why they are useful as seeded
        // blacklist test data (see README.md "Test data").
        var ssn = Ssn.Create("111-11-1111");

        Assert.Equal("111111111", ssn.Digits);
    }

    [Fact]
    public void ToString_MasksAllButLast4()
    {
        var ssn = Ssn.Create("555-55-5555");

        Assert.Equal("***-**-5555", ssn.ToString());
    }
}
