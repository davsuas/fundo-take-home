using Fundo.Loans.Domain.ValueObjects;
using Fundo.Loans.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace Fundo.Loans.Infrastructure.IntegrationTests.Security;

/// <summary>Pure logic, no container needed — kept in this project because there is no separate Infrastructure.UnitTests project (see ARCHITECTURE.md).</summary>
public class HmacSsnHasherTests
{
    private static HmacSsnHasher HasherWithPepper(string pepper) => new(Options.Create(new SsnHashingOptions { Pepper = pepper }));

    [Fact]
    public void Hash_IsDeterministicForSameSsnAndPepper()
    {
        var hasher = HasherWithPepper("pepper-1");
        var ssn = Ssn.Create("555-55-5555");

        var first = hasher.Hash(ssn);
        var second = hasher.Hash(ssn);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Hash_DiffersForDifferentSsns()
    {
        var hasher = HasherWithPepper("pepper-1");

        var a = hasher.Hash(Ssn.Create("555-55-5555"));
        var b = hasher.Hash(Ssn.Create("444-44-4444"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Hash_DiffersForDifferentPepper()
    {
        var ssn = Ssn.Create("555-55-5555");

        var a = HasherWithPepper("pepper-1").Hash(ssn);
        var b = HasherWithPepper("pepper-2").Hash(ssn);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Constructor_ThrowsWhenPepperMissing()
    {
        Assert.Throws<InvalidOperationException>(() => HasherWithPepper(""));
    }

    [Fact]
    public void Hash_NeverContainsPlaintextDigits()
    {
        var hasher = HasherWithPepper("pepper-1");
        var ssn = Ssn.Create("555-55-5555");

        var hash = hasher.Hash(ssn);

        Assert.DoesNotContain(ssn.Digits, hash);
    }
}
