using Fundo.Loans.Application.Decisioning;
using Fundo.Loans.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Xunit;

namespace Fundo.Loans.Application.UnitTests.Decisioning;

public class RestrictedStateRuleTests
{
    private static DecisionRequest RequestFor(string state) => new(
        PersonName.Create("Jane", "Doe"),
        Address.Create("1 Main St", "Springfield", state, "94105"),
        "Acme",
        Ssn.Create("555-55-5555"),
        Money.Create(10_000));

    [Fact]
    public async Task EvaluateAsync_DeniesConfiguredRestrictedState()
    {
        var rule = new RestrictedStateRule(Options.Create(new DecisioningOptions { RestrictedStates = ["NY"] }));

        var decision = await rule.EvaluateAsync(RequestFor("NY"), CancellationToken.None);

        Assert.False(decision.IsApproved);
        Assert.Equal(RestrictedStateRule.RuleCode, decision.RuleCode);
    }

    [Fact]
    public async Task EvaluateAsync_ApprovesNonRestrictedState()
    {
        var rule = new RestrictedStateRule(Options.Create(new DecisioningOptions { RestrictedStates = ["NY"] }));

        var decision = await rule.EvaluateAsync(RequestFor("CA"), CancellationToken.None);

        Assert.True(decision.IsApproved);
    }

    [Fact]
    public async Task EvaluateAsync_RestrictedStatesAreConfigDriven()
    {
        var rule = new RestrictedStateRule(Options.Create(new DecisioningOptions { RestrictedStates = ["TX"] }));

        var deniedDecision = await rule.EvaluateAsync(RequestFor("TX"), CancellationToken.None);
        var approvedDecision = await rule.EvaluateAsync(RequestFor("NY"), CancellationToken.None);

        Assert.False(deniedDecision.IsApproved);
        Assert.True(approvedDecision.IsApproved);
    }

    [Fact]
    public void Order_Is10()
    {
        var rule = new RestrictedStateRule(Options.Create(new DecisioningOptions()));

        Assert.Equal(10, rule.Order);
    }
}
