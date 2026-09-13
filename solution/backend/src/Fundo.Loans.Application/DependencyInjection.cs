using Fundo.Loans.Application.Decisioning;
using Fundo.Loans.Application.LoanApplications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fundo.Loans.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLoansApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DecisioningOptions>()
            .Bind(configuration.GetSection(DecisioningOptions.SectionName));

        services.AddDenyRule<RestrictedStateRule>();
        services.AddDenyRule<BlacklistedSsnRule>();

        // Scoped, not Singleton: BlacklistedSsnRule depends on ISsnBlacklist, which is backed by
        // the scoped DbContext, so RuleEngine must live at the same scope as its rules.
        services.AddScoped<RuleEngine>();
        services.AddScoped<SubmitLoanApplicationHandler>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>
    /// Registers a deny rule. Adding a rule = one new class + one call to this method; neither
    /// the engine nor any existing rule changes.
    /// </summary>
    public static IServiceCollection AddDenyRule<TRule>(this IServiceCollection services)
        where TRule : class, IDenyRule
        => services.AddScoped<IDenyRule, TRule>();
}
