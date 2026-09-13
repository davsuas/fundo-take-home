using Xunit;

namespace Fundo.Loans.Domain.UnitTests;

public class DecisionTests
{
    [Fact]
    public void Approve_HasNoRuleCodeOrReason()
    {
        var decision = Decision.Approve();

        Assert.True(decision.IsApproved);
        Assert.Null(decision.RuleCode);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public void Deny_CarriesRuleCodeAndReason()
    {
        var decision = Decision.Deny("RESTRICTED_STATE", "NY is restricted.");

        Assert.False(decision.IsApproved);
        Assert.Equal("RESTRICTED_STATE", decision.RuleCode);
        Assert.Equal("NY is restricted.", decision.Reason);
    }
}
