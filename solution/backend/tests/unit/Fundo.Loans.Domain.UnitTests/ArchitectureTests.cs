using System.Reflection;
using Fundo.Loans.Domain;
using Xunit;

namespace Fundo.Loans.Domain.UnitTests;

/// <summary>
/// Guards the one architectural rule that matters most: Domain has zero framework dependencies.
/// If someone adds an EF Core or ASP.NET Core reference to Fundo.Loans.Domain.csproj, this test
/// fails the build, not just a code review comment.
/// </summary>
public class ArchitectureTests
{
    [Fact]
    public void Domain_DoesNotReferenceEntityFrameworkCoreOrAspNetCore()
    {
        var domainAssembly = typeof(DomainException).Assembly;

        var forbiddenReferences = domainAssembly.GetReferencedAssemblies()
            .Where(reference =>
                reference.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
                reference.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
                reference.Name!.StartsWith("Npgsql", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToList();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void Domain_OnlyReferencesTheBaseClassLibrary()
    {
        var domainAssembly = typeof(DomainException).Assembly;

        var nonSystemReferences = domainAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && !name.StartsWith("System", StringComparison.Ordinal) && name != "netstandard")
            .ToList();

        Assert.Empty(nonSystemReferences);
    }
}
