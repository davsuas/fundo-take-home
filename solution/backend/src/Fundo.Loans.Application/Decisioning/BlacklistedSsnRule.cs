using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain;

namespace Fundo.Loans.Application.Decisioning;

/// <summary>Denies applications whose SSN hash matches an entry in the blacklist.</summary>
public sealed class BlacklistedSsnRule : IDenyRule
{
    public const string RuleCode = "BLACKLISTED_SSN";

    private readonly ISsnBlacklist _blacklist;

    public BlacklistedSsnRule(ISsnBlacklist blacklist)
    {
        _blacklist = blacklist;
    }

    public string Code => RuleCode;

    public int Order => 20;

    public async Task<Decision> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken)
    {
        var isBlacklisted = await _blacklist.ContainsAsync(request.Ssn, cancellationToken).ConfigureAwait(false);

        return isBlacklisted
            ? Decision.Deny(RuleCode, "This SSN is not eligible for a loan application.")
            : Decision.Approve();
    }
}
