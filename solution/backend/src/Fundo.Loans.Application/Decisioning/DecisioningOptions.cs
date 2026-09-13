namespace Fundo.Loans.Application.Decisioning;

/// <summary>Config-driven knobs for the deny rules. Section: "Decisioning".</summary>
public sealed class DecisioningOptions
{
    public const string SectionName = "Decisioning";

    public string[] RestrictedStates { get; set; } = ["NY"];
}
