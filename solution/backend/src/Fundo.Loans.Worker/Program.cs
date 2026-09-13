using Fundo.Loans.Infrastructure;
using Serilog;
using Serilog.Formatting.Json;

// The worker host: runs the outbox dispatcher and nothing else. It has no HTTP listener.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(configuration => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

builder.Services.AddLoansInfrastructure(builder.Configuration);
builder.Services.AddOutboxDispatcher(builder.Configuration);

await builder.Build().RunAsync();
