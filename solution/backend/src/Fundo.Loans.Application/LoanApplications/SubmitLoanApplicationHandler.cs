using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Application.Contracts;
using Fundo.Loans.Application.Decisioning;
using Fundo.Loans.Domain;
using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Fundo.Loans.Application.LoanApplications;

/// <summary>
/// The single use case: validate -&gt; decide -&gt; (if approved) upsert customer + application and
/// enqueue the outbox event in one transaction. Denied or invalid submissions persist nothing.
/// </summary>
public sealed class SubmitLoanApplicationHandler
{
    public const string CustomerUpsertedEventType = "customer.upserted";

    private readonly RuleEngine _ruleEngine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICustomerRepository _customers;
    private readonly ILoanApplicationRepository _applications;
    private readonly IOutboxWriter _outbox;
    private readonly ISsnHasher _ssnHasher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SubmitLoanApplicationHandler> _logger;

    public SubmitLoanApplicationHandler(
        RuleEngine ruleEngine,
        IUnitOfWork unitOfWork,
        ICustomerRepository customers,
        ILoanApplicationRepository applications,
        IOutboxWriter outbox,
        ISsnHasher ssnHasher,
        TimeProvider timeProvider,
        ILogger<SubmitLoanApplicationHandler> logger)
    {
        _ruleEngine = ruleEngine;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _applications = applications;
        _outbox = outbox;
        _ssnHasher = ssnHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SubmitLoanApplicationResult> HandleAsync(SubmitLoanApplicationCommand command, CancellationToken cancellationToken)
    {
        if (!TryBuildDecisionRequest(command, out var request, out var errors))
        {
            return SubmitLoanApplicationResult.Invalid(errors);
        }

        var decision = await _ruleEngine.DecideAsync(request, cancellationToken).ConfigureAwait(false);

        if (!decision.IsApproved)
        {
            _logger.LogInformation("LoanApplicationDenied ruleCode={RuleCode} ssn={Ssn}", decision.RuleCode, request.Ssn);
            return SubmitLoanApplicationResult.Denied(decision.RuleCode!, decision.Reason!);
        }

        PersistedApplication persisted;
        try
        {
            persisted = await PersistAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            // A concurrent submission with the same SSN committed the customer first. Our
            // transaction was rolled back, so one retry now finds that customer and takes the
            // returning-customer path instead of failing the request.
            _logger.LogInformation("LoanApplicationConcurrentSubmission ssn={Ssn} retrying", request.Ssn);
            persisted = await PersistAsync(request, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "LoanApplicationApproved customerId={CustomerId} applicationId={ApplicationId} isReturning={IsReturning} ssn={Ssn}",
            persisted.CustomerId,
            persisted.ApplicationId,
            persisted.IsReturningCustomer,
            request.Ssn);

        return SubmitLoanApplicationResult.Approved(persisted.ApplicationId, persisted.CustomerId, persisted.IsReturningCustomer);
    }

    private Task<PersistedApplication> PersistAsync(DecisionRequest request, CancellationToken cancellationToken)
        => _unitOfWork.ExecuteAsync(async ct =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var ssnHash = _ssnHasher.Hash(request.Ssn);

            var customer = await _customers.FindBySsnHashAsync(ssnHash, ct).ConfigureAwait(false);
            var isReturningCustomer = customer is not null;

            if (customer is null)
            {
                customer = Customer.Register(request.Name, request.Address, request.CompanyName, ssnHash, request.Ssn.Last4, now);
                _customers.Add(customer);
            }
            else
            {
                customer.UpdateDetails(request.Name, request.Address, request.CompanyName, now);
            }

            var application = await _applications.FindByCustomerIdAsync(customer.Id, ct).ConfigureAwait(false);

            if (application is null)
            {
                application = LoanApplication.Open(customer.Id, request.RequestedAmount, now);
                _applications.Add(application);
            }
            else
            {
                application.ChangeRequestedAmount(request.RequestedAmount, now);
            }

            var payload = BuildPayload(customer, application, isReturningCustomer, now);
            _outbox.Enqueue(OutboxMessage.For(CustomerUpsertedEventType, JsonSerializer.Serialize(payload, OutboxJson.Options), now));

            return new PersistedApplication(customer.Id, application.Id, isReturningCustomer);
        }, cancellationToken);

    private static bool TryBuildDecisionRequest(
        SubmitLoanApplicationCommand command,
        [NotNullWhen(true)] out DecisionRequest? request,
        out IReadOnlyDictionary<string, string[]> errors)
    {
        var collected = new Dictionary<string, string[]>();

        var name = Capture(collected, "name", () => PersonName.Create(command.FirstName, command.LastName));
        var address = Capture(collected, "address", () => Address.Create(command.Street, command.City, command.State, command.PostalCode));
        var ssn = Capture(collected, "ssn", () => Ssn.Create(command.Ssn));
        var amount = Capture(collected, "requestedAmount", () => Money.Create(command.RequestedAmount));

        var companyName = command.CompanyName?.Trim() ?? string.Empty;
        if (companyName.Length is 0 or > 200)
        {
            collected["companyName"] = ["Company name must be between 1 and 200 characters."];
        }

        errors = collected;
        request = collected.Count == 0 ? new DecisionRequest(name!, address!, companyName, ssn!, amount!) : null;
        return request is not null;
    }

    private static T? Capture<T>(Dictionary<string, string[]> errors, string field, Func<T> create)
        where T : class
    {
        try
        {
            return create();
        }
        catch (Exception ex) when (ex is DomainException or ArgumentNullException)
        {
            errors[field] = [ex.Message];
            return null;
        }
    }

    private static CustomerUpsertPayload BuildPayload(Customer customer, LoanApplication application, bool isReturningCustomer, DateTime nowUtc)
        => new(
            new CustomerPayload(
                customer.Id,
                customer.Name.First,
                customer.Name.Last,
                customer.CompanyName,
                customer.SsnLast4,
                new AddressPayload(customer.Address.Street, customer.Address.City, customer.Address.State, customer.Address.PostalCode)),
            new LoanApplicationPayload(application.Id, application.RequestedAmount.Amount, Money.Currency, application.Status.ToString()),
            isReturningCustomer ? "update" : "create",
            nowUtc);

    private sealed record PersistedApplication(Guid CustomerId, Guid ApplicationId, bool IsReturningCustomer);
}
