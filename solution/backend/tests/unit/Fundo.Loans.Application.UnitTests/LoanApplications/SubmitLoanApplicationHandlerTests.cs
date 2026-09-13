using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Application.Decisioning;
using Fundo.Loans.Application.LoanApplications;
using Fundo.Loans.Application.UnitTests.TestDoubles;
using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Fundo.Loans.Application.UnitTests.LoanApplications;

public class SubmitLoanApplicationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static SubmitLoanApplicationCommand ValidCommand(string ssn = "555-55-5555", decimal amount = 10_000, string state = "CA")
        => new("Jane", "Doe", "1 Main St", "Springfield", state, "94105", "Acme", amount, ssn);

    private sealed class Harness
    {
        public ICustomerRepository Customers { get; } = Substitute.For<ICustomerRepository>();
        public ILoanApplicationRepository Applications { get; } = Substitute.For<ILoanApplicationRepository>();
        public IOutboxWriter Outbox { get; } = Substitute.For<IOutboxWriter>();
        public ISsnBlacklist Blacklist { get; } = Substitute.For<ISsnBlacklist>();
        public ISsnHasher Hasher { get; } = Substitute.For<ISsnHasher>();
        public IUnitOfWork UnitOfWork { get; init; } = new FakeUnitOfWork();

        public Harness()
        {
            Hasher.Hash(Arg.Any<Ssn>()).Returns(call => "hash-" + call.Arg<Ssn>().Digits);
            Blacklist.ContainsAsync(Arg.Any<Ssn>(), Arg.Any<CancellationToken>()).Returns(false);
        }

        public SubmitLoanApplicationHandler BuildHandler() => new(
            new RuleEngine([
                new RestrictedStateRule(Options.Create(new DecisioningOptions { RestrictedStates = ["NY"] })),
                new BlacklistedSsnRule(Blacklist),
            ]),
            UnitOfWork,
            Customers,
            Applications,
            Outbox,
            Hasher,
            new FixedTimeProvider(Now),
            NullLogger<SubmitLoanApplicationHandler>.Instance);
    }

    private static (Customer Customer, LoanApplication Application) ExistingCustomerWithApplication()
    {
        var customer = Customer.Register(
            PersonName.Create("Old", "Name"),
            Address.Create("9 Old St", "Oldtown", "CA", "90001"),
            "OldCo",
            "hash-555555555",
            "5555",
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        return (customer, LoanApplication.Open(customer.Id, Money.Create(5_000), customer.CreatedAtUtc));
    }

    [Fact]
    public async Task HandleAsync_NewCustomer_CreatesCustomerAndApplicationAndEnqueuesCreateEvent()
    {
        var harness = new Harness();

        var result = await harness.BuildHandler().HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Approved, result.Outcome);
        Assert.False(result.IsReturningCustomer);
        Assert.NotNull(result.CustomerId);
        Assert.NotNull(result.ApplicationId);
        harness.Customers.Received(1).Add(Arg.Is<Customer>(c => c.SsnHash == "hash-555555555" && c.SsnLast4 == "5555"));
        harness.Applications.Received(1).Add(Arg.Is<LoanApplication>(a => a.RequestedAmount.Amount == 10_000m));
        harness.Outbox.Received(1).Enqueue(Arg.Is<OutboxMessage>(m =>
            m.Type == SubmitLoanApplicationHandler.CustomerUpsertedEventType && m.Payload.Contains("\"operation\":\"create\"")));
    }

    [Fact]
    public async Task HandleAsync_ReturningCustomer_UpdatesExistingRecordsAndEnqueuesUpdateEvent()
    {
        var harness = new Harness();
        var (customer, application) = ExistingCustomerWithApplication();
        harness.Customers.FindBySsnHashAsync("hash-555555555", Arg.Any<CancellationToken>()).Returns(customer);
        harness.Applications.FindByCustomerIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(application);

        var result = await harness.BuildHandler().HandleAsync(ValidCommand(amount: 30_000), CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Approved, result.Outcome);
        Assert.True(result.IsReturningCustomer);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(application.Id, result.ApplicationId);
        Assert.Equal("Jane", customer.Name.First);
        Assert.Equal("Acme", customer.CompanyName);
        Assert.Equal(30_000m, application.RequestedAmount.Amount);
        harness.Customers.DidNotReceive().Add(Arg.Any<Customer>());
        harness.Applications.DidNotReceive().Add(Arg.Any<LoanApplication>());
        harness.Outbox.Received(1).Enqueue(Arg.Is<OutboxMessage>(m => m.Payload.Contains("\"operation\":\"update\"")));
    }

    [Theory]
    [InlineData("555-55-5555", "NY", RestrictedStateRule.RuleCode)]
    [InlineData("111-11-1111", "CA", BlacklistedSsnRule.RuleCode)]
    public async Task HandleAsync_Denied_PersistsNothingAndEnqueuesNoEvent(string ssn, string state, string expectedRuleCode)
    {
        var harness = new Harness();
        harness.Blacklist.ContainsAsync(Arg.Is<Ssn>(s => s.Digits == "111111111"), Arg.Any<CancellationToken>()).Returns(true);
        var unitOfWork = (FakeUnitOfWork)harness.UnitOfWork;

        var result = await harness.BuildHandler().HandleAsync(ValidCommand(ssn: ssn, state: state), CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Denied, result.Outcome);
        Assert.Equal(expectedRuleCode, result.RuleCode);
        Assert.Equal(0, unitOfWork.Executions);
        harness.Outbox.DidNotReceive().Enqueue(Arg.Any<OutboxMessage>());
    }

    [Fact]
    public async Task HandleAsync_InvalidInput_ReturnsFieldErrorsWithoutRunningRulesOrPersisting()
    {
        var harness = new Harness();

        var result = await harness.BuildHandler().HandleAsync(ValidCommand(ssn: "123", amount: 0), CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Invalid, result.Outcome);
        Assert.Contains("ssn", result.Errors.Keys);
        Assert.Contains("requestedAmount", result.Errors.Keys);
        await harness.Blacklist.DidNotReceive().ContainsAsync(Arg.Any<Ssn>(), Arg.Any<CancellationToken>());
        harness.Outbox.DidNotReceive().Enqueue(Arg.Any<OutboxMessage>());
    }

    [Fact]
    public async Task HandleAsync_WhenCommitFails_PropagatesTheFailureInsteadOfReportingApproval()
    {
        var harness = new Harness { UnitOfWork = new FailingCommitUnitOfWork() };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.BuildHandler().HandleAsync(ValidCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WhenAConcurrentSubmissionWinsTheInsert_RetriesOnceAsReturningCustomer()
    {
        var unitOfWork = new ConflictOnFirstCommitUnitOfWork();
        var harness = new Harness { UnitOfWork = unitOfWork };
        var (customer, application) = ExistingCustomerWithApplication();

        // First attempt sees no customer (the rival has not committed yet); the retry sees it.
        harness.Customers.FindBySsnHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Customer?)null, customer);
        harness.Applications.FindByCustomerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LoanApplication?)null, application);

        var result = await harness.BuildHandler().HandleAsync(ValidCommand(amount: 20_000), CancellationToken.None);

        Assert.Equal(2, unitOfWork.Executions);
        Assert.Equal(SubmissionOutcome.Approved, result.Outcome);
        Assert.True(result.IsReturningCustomer);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(20_000m, application.RequestedAmount.Amount);
    }
}
