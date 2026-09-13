namespace Fundo.Loans.Domain;

/// <summary>The outcome of running <c>RuleEngine.DecideAsync</c> — approve, or the first rule that denied.</summary>
public sealed record Decision
{
    public bool IsApproved { get; }
    public string? RuleCode { get; }
    public string? Reason { get; }

    private Decision(bool isApproved, string? ruleCode, string? reason)
    {
        IsApproved = isApproved;
        RuleCode = ruleCode;
        Reason = reason;
    }

    public static Decision Approve() => new(true, null, null);

    public static Decision Deny(string ruleCode, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new Decision(false, ruleCode, reason);
    }
}
