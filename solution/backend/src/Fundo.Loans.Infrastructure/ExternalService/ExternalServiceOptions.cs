namespace Fundo.Loans.Infrastructure.ExternalService;

public sealed class ExternalServiceOptions
{
    public const string SectionName = "ExternalService";

    /// <summary>Base URL of the external service, e.g. http://external-service:4000/.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Per-request timeout; bounds how long one delivery can take (see OutboxOptions.ClaimLease).</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}
