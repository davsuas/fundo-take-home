using Fundo.Loans.Domain;
using Microsoft.Extensions.Options;

namespace Fundo.Loans.Application.Decisioning;

/// <summary>Denies applications from a configured set of states (default: NY only).</summary>
public sealed class RestrictedStateRule : IDenyRule
{
    public const string RuleCode = "RESTRICTED_STATE";

    private readonly DecisioningOptions _options;

    public RestrictedStateRule(IOptions<DecisioningOptions> options)
    {
        _options = options.Value;
    }

    public string Code => RuleCode;

    public int Order => 10;

    public Task<Decision> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken)
    {
        var isRestricted = _options.RestrictedStates
            .Any(state => string.Equals(state, request.Address.State, StringComparison.OrdinalIgnoreCase));

        var decision = isRestricted
            ? Decision.Deny(RuleCode, $"Applications from {request.Address.State} are not accepted.")
            : Decision.Approve();

        return Task.FromResult(decision);
    }
}
