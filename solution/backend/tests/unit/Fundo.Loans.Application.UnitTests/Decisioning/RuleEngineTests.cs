using Fundo.Loans.Application.Decisioning;
using Fundo.Loans.Domain;
using Fundo.Loans.Domain.ValueObjects;
using Xunit;

namespace Fundo.Loans.Application.UnitTests.Decisioning;

public class RuleEngineTests
{
    private static readonly DecisionRequest AnyRequest = new(
        PersonName.Create("Jane", "Doe"),
        Address.Create("1 Main St", "Springfield", "CA", "94105"),
        "Acme",
        Ssn.Create("555-55-5555"),
        Money.Create(10_000));

    private sealed class RecordingRule(string code, int order, bool deny) : IDenyRule
    {
        public List<string> Calls { get; } = [];

        public string Code => code;
        public int Order => order;

        public Task<Decision> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken)
        {
            Calls.Add(code);
            return Task.FromResult(deny ? Decision.Deny(code, $"{code} denied") : Decision.Approve());
        }
    }

    [Fact]
    public async Task DecideAsync_ApprovesWhenNoRuleDenies()
    {
        var engine = new RuleEngine([new RecordingRule("A", 1, false), new RecordingRule("B", 2, false)]);

        var decision = await engine.DecideAsync(AnyRequest, CancellationToken.None);

        Assert.True(decision.IsApproved);
    }

    [Fact]
    public async Task DecideAsync_FirstDenyingRuleWins()
    {
        var first = new RecordingRule("FIRST", 1, deny: true);
        var second = new RecordingRule("SECOND", 2, deny: true);
        var engine = new RuleEngine([second, first]); // registered out of order on purpose

        var decision = await engine.DecideAsync(AnyRequest, CancellationToken.None);

        Assert.False(decision.IsApproved);
        Assert.Equal("FIRST", decision.RuleCode);
    }

    [Fact]
    public async Task DecideAsync_ShortCircuitsAfterFirstDeny()
    {
        var first = new RecordingRule("FIRST", 1, deny: true);
        var second = new RecordingRule("SECOND", 2, deny: true);
        var engine = new RuleEngine([first, second]);

        await engine.DecideAsync(AnyRequest, CancellationToken.None);

        Assert.Single(first.Calls);
        Assert.Empty(second.Calls);
    }

    [Fact]
    public async Task DecideAsync_RunsRulesInOrderRegardlessOfRegistrationOrder()
    {
        var callOrder = new List<string>();
        var first = new OrderTrackingRule("FIRST", 1, callOrder);
        var second = new OrderTrackingRule("SECOND", 2, callOrder);
        var engine = new RuleEngine([second, first]);

        await engine.DecideAsync(AnyRequest, CancellationToken.None);

        Assert.Equal(["FIRST", "SECOND"], callOrder);
    }

    private sealed class OrderTrackingRule(string code, int order, List<string> callOrder) : IDenyRule
    {
        public string Code => code;
        public int Order => order;

        public Task<Decision> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken)
        {
            callOrder.Add(code);
            return Task.FromResult(Decision.Approve());
        }
    }
}
