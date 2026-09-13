using Fundo.Loans.Api.Contracts;
using Fundo.Loans.Application.LoanApplications;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Fundo.Loans.Api.Endpoints;

/// <summary>Thin endpoint: map the request to a command, delegate to the handler, map the result to HTTP. No business rules here.</summary>
public static class LoanApplicationsEndpoints
{
    public static IEndpointRouteBuilder MapLoanApplicationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/loan-applications", SubmitAsync).WithName("SubmitLoanApplication");
        return app;
    }

    private static async Task<Results<Ok<SubmitLoanApplicationResponse>, ValidationProblem>> SubmitAsync(
        SubmitLoanApplicationRequest request,
        SubmitLoanApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitLoanApplicationCommand(
            request.FirstName,
            request.LastName,
            request.Street,
            request.City,
            request.State,
            request.PostalCode,
            request.CompanyName,
            request.RequestedAmount,
            request.Ssn);

        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        if (result.Outcome == SubmissionOutcome.Invalid)
        {
            return TypedResults.ValidationProblem(result.Errors);
        }

        return TypedResults.Ok(new SubmitLoanApplicationResponse(
            result.Outcome.ToString(),
            result.ApplicationId,
            result.CustomerId,
            result.IsReturningCustomer,
            result.RuleCode,
            result.Reason));
    }
}
