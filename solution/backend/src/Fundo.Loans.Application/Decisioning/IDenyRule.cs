using Fundo.Loans.Domain;

namespace Fundo.Loans.Application.Decisioning;

/// <summary>
/// One deny rule. Adding a new rule = a new class implementing this interface + one
/// `services.AddDenyRule&lt;TRule&gt;()` line in <see cref="DependencyInjection"/>. No existing
/// rule, and no line inside <see cref="RuleEngine"/>, ever changes.
/// </summary>
public interface IDenyRule
{
    /// <summary>Stable machine-readable reason code, e.g. "RESTRICTED_STATE".</summary>
    string Code { get; }

    /// <summary>Evaluation order — lower runs first. Rules should not depend on ordering for correctness, only for which reason surfaces first.</summary>
    int Order { get; }

    Task<Decision> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken);
}
