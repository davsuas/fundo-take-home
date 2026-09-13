using Fundo.Loans.Application.LoanApplications;
using Xunit;

namespace Fundo.Loans.Application.UnitTests;

/// <summary>Dependencies point inward: Application knows Domain and nothing about persistence, HTTP or hosting frameworks.</summary>
public class ArchitectureTests
{
    [Fact]
    public void Application_DoesNotReferenceInfrastructureOrFrameworks()
    {
        var forbidden = new[] { "Fundo.Loans.Infrastructure", "Fundo.Loans.Api", "Fundo.Loans.Worker", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql" };

        var violations = typeof(SubmitLoanApplicationHandler).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => forbidden.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(violations);
    }
}
