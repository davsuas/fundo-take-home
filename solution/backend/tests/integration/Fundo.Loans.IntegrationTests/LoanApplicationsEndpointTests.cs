using System.Net;
using System.Net.Http.Json;
using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain.ValueObjects;
using Fundo.Loans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fundo.Loans.IntegrationTests;

/// <summary>POST /api/v1/loan-applications end to end: HTTP -&gt; handler -&gt; rule engine -&gt; Postgres (real transaction, real unique indexes).</summary>
public sealed class LoanApplicationsEndpointTests : IClassFixture<ApiFactory>
{
    private const string Endpoint = "/api/v1/loan-applications";

    private readonly ApiFactory _factory;

    public LoanApplicationsEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record SubmitRequest(
        string FirstName, string LastName, string Street, string City, string State,
        string PostalCode, string CompanyName, decimal RequestedAmount, string Ssn);

    private sealed record SubmitResponse(string Outcome, Guid? ApplicationId, Guid? CustomerId, bool IsReturningCustomer, string? RuleCode, string? Reason);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string UniqueSsn() => $"5{Random.Shared.Next(10_000_000, 99_999_999)}";

    private static SubmitRequest ValidRequest(string ssn, string state = "CA", decimal amount = 15_000)
        => new("Jane", "Doe", "1 Main St", "Springfield", state, "94105", "Acme", amount, ssn);

    [Fact]
    public async Task Post_NewCustomer_ApprovesAndPersistsCustomerApplicationAndEvent()
    {
        using var client = _factory.CreateClient();
        var ssn = UniqueSsn();

        var response = await client.PostAsJsonAsync(Endpoint, ValidRequest(ssn), Ct);
        var body = await response.Content.ReadFromJsonAsync<SubmitResponse>(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Approved", body!.Outcome);
        Assert.False(body.IsReturningCustomer);

        var state = await LoadStateAsync(ssn);
        Assert.Equal(1, state.Customers);
        Assert.Equal(1, state.Applications);
        Assert.Equal(["create"], state.EventOperations);
    }

    [Fact]
    public async Task Post_StateNy_IsDeniedAndPersistsNothing()
    {
        using var client = _factory.CreateClient();
        var ssn = UniqueSsn();

        var response = await client.PostAsJsonAsync(Endpoint, ValidRequest(ssn, state: "NY"), Ct);
        var body = await response.Content.ReadFromJsonAsync<SubmitResponse>(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Denied", body!.Outcome);
        Assert.Equal("RESTRICTED_STATE", body.RuleCode);
        Assert.Equal(0, (await LoadStateAsync(ssn)).Customers);
    }

    [Fact]
    public async Task Post_SeededBlacklistedSsn_IsDeniedAndPersistsNothing()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(Endpoint, ValidRequest("111-11-1111"), Ct);
        var body = await response.Content.ReadFromJsonAsync<SubmitResponse>(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Denied", body!.Outcome);
        Assert.Equal("BLACKLISTED_SSN", body.RuleCode);
        Assert.Equal(0, (await LoadStateAsync("111-11-1111")).Customers);
    }

    [Fact]
    public async Task Post_SameSsnTwice_UpdatesTheSingleCustomerAndApplicationAndEmitsAnUpdateEvent()
    {
        using var client = _factory.CreateClient();
        var ssn = UniqueSsn();

        var first = await (await client.PostAsJsonAsync(Endpoint, ValidRequest(ssn, amount: 10_000), Ct)).Content.ReadFromJsonAsync<SubmitResponse>(Ct);
        var second = await (await client.PostAsJsonAsync(Endpoint, ValidRequest(ssn, state: "TX", amount: 40_000), Ct)).Content.ReadFromJsonAsync<SubmitResponse>(Ct);

        Assert.False(first!.IsReturningCustomer);
        Assert.True(second!.IsReturningCustomer);
        Assert.Equal(first.CustomerId, second.CustomerId);
        Assert.Equal(first.ApplicationId, second.ApplicationId);

        var state = await LoadStateAsync(ssn);
        Assert.Equal(1, state.Customers);
        Assert.Equal(1, state.Applications);
        Assert.Equal(40_000m, state.RequestedAmount);
        Assert.Equal("TX", state.CustomerState);
        Assert.Equal(["create", "update"], state.EventOperations);
    }

    [Fact]
    public async Task Post_SameSsnConcurrently_AllSucceedWithOneCustomerAndOneApplication()
    {
        var ssn = UniqueSsn();

        var responses = await Task.WhenAll(Enumerable.Range(1, 8).Select(async i =>
        {
            using var client = _factory.CreateClient();
            return await client.PostAsJsonAsync(Endpoint, ValidRequest(ssn, amount: 1_000 * i), Ct);
        }));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        var state = await LoadStateAsync(ssn);
        Assert.Equal(1, state.Customers);
        Assert.Equal(1, state.Applications);
    }

    [Fact]
    public async Task Post_InvalidInput_Returns400WithFieldErrors()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(Endpoint, ValidRequest("not-nine-digits"), Ct);
        var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty("ssn", out _));
    }

    private sealed record PersistedState(int Customers, int Applications, decimal? RequestedAmount, string? CustomerState, string[] EventOperations);

    private async Task<PersistedState> LoadStateAsync(string rawSsn)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LoansDbContext>();
        var ssnHash = scope.ServiceProvider.GetRequiredService<ISsnHasher>().Hash(Ssn.Create(rawSsn));

        var customers = await dbContext.Customers.AsNoTracking().Where(c => c.SsnHash == ssnHash).ToListAsync(Ct);
        var customerIds = customers.Select(c => c.Id).ToList();
        var applications = await dbContext.LoanApplications.AsNoTracking().Where(a => customerIds.Contains(a.CustomerId)).ToListAsync(Ct);

        var idFragments = customerIds.Select(id => id.ToString()).ToList();
        var events = (await dbContext.OutboxMessages.AsNoTracking().OrderBy(m => m.OccurredAtUtc).ToListAsync(Ct))
            .Where(m => idFragments.Any(id => m.Payload.Contains(id, StringComparison.Ordinal)))
            .Select(m => System.Text.Json.Nodes.JsonNode.Parse(m.Payload)!["operation"]!.GetValue<string>())
            .ToArray();

        return new PersistedState(
            customers.Count,
            applications.Count,
            applications.SingleOrDefault()?.RequestedAmount.Amount,
            customers.SingleOrDefault()?.Address.State,
            events);
    }
}
