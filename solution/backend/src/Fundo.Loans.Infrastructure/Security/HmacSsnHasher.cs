using System.Security.Cryptography;
using System.Text;
using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Fundo.Loans.Infrastructure.Security;

/// <summary>
/// Deterministic HMAC-SHA256 hash of the SSN's 9 digits, keyed by a server-side pepper
/// (SSN_HASH_PEPPER). Deterministic so the same SSN always hashes to the same value (needed for
/// customer lookup and blacklist comparison) while the plaintext SSN is never persisted anywhere.
/// </summary>
public sealed class HmacSsnHasher : ISsnHasher
{
    private readonly byte[] _key;

    public HmacSsnHasher(IOptions<SsnHashingOptions> options)
    {
        if (string.IsNullOrWhiteSpace(options.Value.Pepper))
        {
            throw new InvalidOperationException("SSN_HASH_PEPPER must be configured.");
        }

        _key = Encoding.UTF8.GetBytes(options.Value.Pepper);
    }

    public string Hash(Ssn ssn)
    {
        var bytes = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(ssn.Digits));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
