using Fundo.Loans.Api.Endpoints;
using Fundo.Loans.Api.HealthChecks;
using Fundo.Loans.Application;
using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Infrastructure;
using Fundo.Loans.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Json;

// `dotnet Fundo.Loans.Api.dll migrate` — run once by the compose `migrate` service with the
// schema-owner role. The long-running api/worker processes never change the schema.
if (args is ["migrate", ..])
{
    await RunMigrationsAsync(args);
    return 0;
}

// `dotnet Fundo.Loans.Api.dll --health-check` — the container HEALTHCHECK. The runtime image has
// no curl/wget, so the app probes its own liveness endpoint over loopback.
if (args is ["--health-check"])
{
    return await ProbeLivenessAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(configuration => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

builder.Services.AddLoansApplication(builder.Configuration);
builder.Services.AddLoansInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapLoanApplicationsEndpoints();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
return 0;

static async Task RunMigrationsAsync(string[] args)
{
    var hostBuilder = Host.CreateApplicationBuilder(args);
    hostBuilder.Services.AddLoansInfrastructure(hostBuilder.Configuration);

    using var host = hostBuilder.Build();
    using var scope = host.Services.CreateScope();

    await Migrator.MigrateAndSeedAsync(
        scope.ServiceProvider.GetRequiredService<LoansDbContext>(),
        scope.ServiceProvider.GetRequiredService<ISsnHasher>(),
        CancellationToken.None);
}

static async Task<int> ProbeLivenessAsync()
{
    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        using var response = await client.GetAsync(new Uri("http://127.0.0.1:8080/health/live"));
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        return 1;
    }
    catch (TaskCanceledException)
    {
        return 1;
    }
}

/// <summary>Exposed so integration tests can host the API with <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
