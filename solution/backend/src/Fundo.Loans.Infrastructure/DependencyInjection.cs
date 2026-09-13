using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Infrastructure.ExternalService;
using Fundo.Loans.Infrastructure.Outbox;
using Fundo.Loans.Infrastructure.Persistence;
using Fundo.Loans.Infrastructure.Persistence.Repositories;
using Fundo.Loans.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Fundo.Loans.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Persistence adapters both hosts need: DbContext, repositories, unit of work, outbox writer, SSN hashing and blacklist.</summary>
    public static IServiceCollection AddLoansInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // The connection string is read inside the callback (when the DbContext is first
        // resolved), so test hosts can override configuration after registration.
        services.AddDbContext<LoansDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Loans")
                ?? throw new InvalidOperationException("Connection string 'Loans' is not configured.");
            options.UseNpgsql(connectionString);
        });

        services.AddOptions<SsnHashingOptions>().Bind(configuration.GetSection(SsnHashingOptions.SectionName));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ISsnBlacklist, EfSsnBlacklist>();
        services.AddSingleton<ISsnHasher, HmacSsnHasher>();

        return services;
    }

    /// <summary>Worker-only: the external-service HTTP adapter and the background dispatcher. The API never calls this.</summary>
    public static IServiceCollection AddOutboxDispatcher(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OutboxOptions>().Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.AddOptions<ExternalServiceOptions>().Bind(configuration.GetSection(ExternalServiceOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient<IExternalCustomerRegistry, HttpExternalCustomerRegistry>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ExternalServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
        });

        services.AddHostedService<OutboxDispatcher>();

        return services;
    }
}
