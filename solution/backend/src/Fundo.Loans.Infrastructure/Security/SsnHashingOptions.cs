namespace Fundo.Loans.Infrastructure.Security;

public sealed class SsnHashingOptions
{
    public const string SectionName = "SsnHashing";

    /// <summary>Secret pepper mixed into the HMAC key. Required — set via the SSN_HASH_PEPPER env var. Never committed.</summary>
    public string Pepper { get; set; } = string.Empty;
}
