using Fundo.Loans.Domain;

namespace Fundo.Loans.Application.Decisioning;

/// <summary>
/// Runs every registered <see cref="IDenyRule"/> in <c>Order</c>. The first rule that denies
/// wins and short-circuits the rest; if none deny, the application is approved. This is the
/// single place deny/approve is decided — controllers and handlers never branch on business
/// conditions themselves.
/// </summary>
public sealed class RuleEngine
{
    private readonly IReadOnlyList<IDenyRule> _rules;

    public RuleEngine(IEnumerable<IDenyRule> rules)
    {
        _rules = rules.OrderBy(r => r.Order).ToArray();
    }

    public async Task<Decision> DecideAsync(DecisionRequest request, CancellationToken cancellationToken)
    {
        foreach (var rule in _rules)
        {
            var decision = await rule.EvaluateAsync(request, cancellationToken).ConfigureAwait(false);

            if (!decision.IsApproved)
            {
                return decision;
            }
        }

        return Decision.Approve();
    }
}
