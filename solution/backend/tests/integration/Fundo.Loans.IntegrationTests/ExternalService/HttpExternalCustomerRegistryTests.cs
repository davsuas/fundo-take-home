using System.Net;
using System.Text.Json;
using Fundo.Loans.Application.Contracts;
using Fundo.Loans.Infrastructure.ExternalService;
using Xunit;

namespace Fundo.Loans.IntegrationTests.ExternalService;

/// <summary>The HTTP contract with the external service, verified at the HttpClient boundary.</summary>
public sealed class HttpExternalCustomerRegistryTests
{
    private static readonly CustomerUpsertPayload Payload = new(
        new CustomerPayload(Guid.Parse("018f2f3a-1b1b-7000-8000-000000000001"), "Jane", "Doe", "Acme", "5555", new AddressPayload("1 Main St", "Springfield", "CA", "94105")),
        new LoanApplicationPayload(Guid.Parse("018f2f3a-1b1b-7000-8000-000000000002"), 25_000m, "USD", "Approved"),
        "update",
        new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task UpsertAsync_PutsThePayloadToTheCustomerUrlWithTheIdempotencyKey()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        var registry = new HttpExternalCustomerRegistry(new HttpClient(handler) { BaseAddress = new Uri("http://external/") });

        await registry.UpsertAsync(Payload, "message-1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Put, handler.Request!.Method);
        Assert.Equal("http://external/api/v1/customers/018f2f3a-1b1b-7000-8000-000000000001", handler.Request.RequestUri!.ToString());
        Assert.Equal("message-1", handler.Request.Headers.GetValues("Idempotency-Key").Single());

        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("update", body.RootElement.GetProperty("operation").GetString());
        Assert.Equal(25_000m, body.RootElement.GetProperty("application").GetProperty("requestedAmount").GetDecimal());
    }

    [Fact]
    public async Task UpsertAsync_WhenTheServiceFails_Throws()
    {
        var registry = new HttpExternalCustomerRegistry(new HttpClient(new StubHandler(HttpStatusCode.InternalServerError)) { BaseAddress = new Uri("http://external/") });

        await Assert.ThrowsAsync<HttpRequestException>(() => registry.UpsertAsync(Payload, "message-1", TestContext.Current.CancellationToken));
    }

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status);
        }
    }
}
