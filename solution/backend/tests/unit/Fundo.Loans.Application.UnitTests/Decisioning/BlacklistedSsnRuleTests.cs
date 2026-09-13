using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Application.Decisioning;
using Fundo.Loans.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Fundo.Loans.Application.UnitTests.Decisioning;

public class BlacklistedSsnRuleTests
{
    private static DecisionRequest RequestFor(string ssn) => new(
        PersonName.Create("Jane", "Doe"),
        Address.Create("1 Main St", "Springfield", "CA", "94105"),
        "Acme",
        Ssn.Create(ssn),
        Money.Create(10_000));

    [Fact]
    public async Task EvaluateAsync_DeniesWhenBlacklistContainsSsn()
    {
        var blacklist = Substitute.For<ISsnBlacklist>();
        blacklist.ContainsAsync(Arg.Any<Ssn>(), Arg.Any<CancellationToken>()).Returns(true);
        var rule = new BlacklistedSsnRule(blacklist);

        var decision = await rule.EvaluateAsync(RequestFor("111-11-1111"), CancellationToken.None);

        Assert.False(decision.IsApproved);
        Assert.Equal(BlacklistedSsnRule.RuleCode, decision.RuleCode);
    }

    [Fact]
    public async Task EvaluateAsync_ApprovesWhenNotBlacklisted()
    {
        var blacklist = Substitute.For<ISsnBlacklist>();
        blacklist.ContainsAsync(Arg.Any<Ssn>(), Arg.Any<CancellationToken>()).Returns(false);
        var rule = new BlacklistedSsnRule(blacklist);

        var decision = await rule.EvaluateAsync(RequestFor("555-55-5555"), CancellationToken.None);

        Assert.True(decision.IsApproved);
    }

    [Fact]
    public void Order_Is20()
    {
        var rule = new BlacklistedSsnRule(Substitute.For<ISsnBlacklist>());

        Assert.Equal(20, rule.Order);
    }
}
