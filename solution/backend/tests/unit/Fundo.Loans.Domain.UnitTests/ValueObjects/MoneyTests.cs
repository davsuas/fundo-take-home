using Fundo.Loans.Domain;
using Fundo.Loans.Domain.ValueObjects;
using Xunit;

namespace Fundo.Loans.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_AcceptsPositiveAmountWithinLimit()
    {
        var money = Money.Create(25_000.005m);

        Assert.Equal(25_000.00m, money.Amount);
        Assert.Equal("USD", Money.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RejectsZeroOrNegative(decimal amount)
    {
        Assert.Throws<DomainException>(() => Money.Create(amount));
    }

    [Fact]
    public void Create_RejectsAboveOneMillion()
    {
        Assert.Throws<DomainException>(() => Money.Create(1_000_000.01m));
    }

    [Fact]
    public void Create_AcceptsExactlyOneMillion()
    {
        var money = Money.Create(1_000_000m);

        Assert.Equal(1_000_000m, money.Amount);
    }
}
