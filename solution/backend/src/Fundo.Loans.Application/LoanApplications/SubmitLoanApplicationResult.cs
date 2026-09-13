namespace Fundo.Loans.Application.LoanApplications;

public enum SubmissionOutcome
{
    Approved,
    Denied,
    Invalid,
}

/// <summary>
/// Every way a submission can end, as data rather than exceptions: approved (with the ids),
/// denied (with the rule that denied it) or invalid (with per-field errors).
/// </summary>
public sealed record SubmitLoanApplicationResult
{
    public required SubmissionOutcome Outcome { get; init; }
    public Guid? ApplicationId { get; init; }
    public Guid? CustomerId { get; init; }
    public bool IsReturningCustomer { get; init; }
    public string? RuleCode { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();

    public static SubmitLoanApplicationResult Approved(Guid applicationId, Guid customerId, bool isReturningCustomer) => new()
    {
        Outcome = SubmissionOutcome.Approved,
        ApplicationId = applicationId,
        CustomerId = customerId,
        IsReturningCustomer = isReturningCustomer,
    };

    public static SubmitLoanApplicationResult Denied(string ruleCode, string reason) => new()
    {
        Outcome = SubmissionOutcome.Denied,
        RuleCode = ruleCode,
        Reason = reason,
    };

    public static SubmitLoanApplicationResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new()
    {
        Outcome = SubmissionOutcome.Invalid,
        Errors = errors,
    };
}
