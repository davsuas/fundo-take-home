using System.Net.Http.Json;
using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Application.Contracts;

namespace Fundo.Loans.Infrastructure.ExternalService;

/// <summary>
/// HTTP adapter for <see cref="IExternalCustomerRegistry"/>: <c>PUT /api/v1/customers/{id}</c>
/// with an <c>Idempotency-Key</c> header. Any non-2xx response throws, and the outbox dispatcher
/// turns that into a scheduled retry — there is deliberately no second retry layer here.
/// </summary>
public sealed class HttpExternalCustomerRegistry : IExternalCustomerRegistry
{
    private readonly HttpClient _httpClient;

    public HttpExternalCustomerRegistry(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task UpsertAsync(CustomerUpsertPayload payload, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/customers/{payload.Customer.Id}")
        {
            Content = JsonContent.Create(payload, options: OutboxJson.Options),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
