using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Domain.ValueObjects;
using Fundo.Loans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fundo.Loans.IntegrationTests.Persistence;

public sealed class UnitOfWorkTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public UnitOfWorkTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Customer NewCustomer(string ssnHash) => Customer.Register(
        PersonName.Create("Jane", "Doe"),
        Address.Create("1 Main St", "Springfield", "CA", "94105"),
        "Acme",
        ssnHash,
        "5555",
        DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_CommitsCustomerApplicationAndOutboxTogether()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var ssnHash = $"commit-{Guid.NewGuid()}";
        var customer = NewCustomer(ssnHash);
        var message = OutboxMessage.For("customer.upserted", "{}", DateTime.UtcNow);

        await new EfUnitOfWork(dbContext).ExecuteAsync(_ =>
        {
            dbContext.Customers.Add(customer);
            dbContext.LoanApplications.Add(LoanApplication.Open(customer.Id, Money.Create(10_000), DateTime.UtcNow));
            dbContext.OutboxMessages.Add(message);
            return Task.FromResult(true);
        }, Ct);

        await using var verify = _fixture.CreateDbContext();
        Assert.Equal(1, await verify.Customers.CountAsync(c => c.SsnHash == ssnHash, Ct));
        Assert.Equal(1, await verify.LoanApplications.CountAsync(a => a.CustomerId == customer.Id, Ct));
        Assert.Equal(1, await verify.OutboxMessages.CountAsync(m => m.Id == message.Id, Ct));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveFails_RollsBackCustomerApplicationAndOutbox()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var ssnHash = $"rollback-{Guid.NewGuid()}";
        var customer = NewCustomer(ssnHash);
        var message = OutboxMessage.For("customer.upserted", "{}", DateTime.UtcNow);

        await Assert.ThrowsAsync<DbUpdateException>(() => new EfUnitOfWork(dbContext).ExecuteAsync(_ =>
        {
            dbContext.Customers.Add(customer);
            dbContext.OutboxMessages.Add(message);

            // An application pointing at a customer that does not exist violates the FK, so
            // SaveChanges fails after the customer and outbox row were already staged.
            dbContext.LoanApplications.Add(LoanApplication.Open(Guid.CreateVersion7(), Money.Create(1), DateTime.UtcNow));
            return Task.FromResult(true);
        }, Ct));

        await using var verify = _fixture.CreateDbContext();
        Assert.Equal(0, await verify.Customers.CountAsync(c => c.SsnHash == ssnHash, Ct));
        Assert.Equal(0, await verify.OutboxMessages.CountAsync(m => m.Id == message.Id, Ct));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSsnHashAlreadyExists_ThrowsConcurrencyConflictAndCanRetryOnTheSameContext()
    {
        var ssnHash = $"conflict-{Guid.NewGuid()}";
        await using (var rival = _fixture.CreateDbContext())
        {
            rival.Customers.Add(NewCustomer(ssnHash));
            await rival.SaveChangesAsync(Ct);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var unitOfWork = new EfUnitOfWork(dbContext);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => unitOfWork.ExecuteAsync(_ =>
        {
            dbContext.Customers.Add(NewCustomer(ssnHash));
            return Task.FromResult(true);
        }, Ct));

        // The failed insert must not linger in the change tracker, or the retry would fail again.
        var found = await unitOfWork.ExecuteAsync(ct => dbContext.Customers.AnyAsync(c => c.SsnHash == ssnHash, ct), Ct);
        Assert.True(found);
    }

    [Fact]
    public async Task UniqueIndex_OnCustomerId_PreventsASecondApplicationForTheSameCustomer()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var customer = NewCustomer($"unique-app-{Guid.NewGuid()}");
        dbContext.Customers.Add(customer);
        dbContext.LoanApplications.Add(LoanApplication.Open(customer.Id, Money.Create(1_000), DateTime.UtcNow));
        await dbContext.SaveChangesAsync(Ct);

        await using var second = _fixture.CreateDbContext();
        second.LoanApplications.Add(LoanApplication.Open(customer.Id, Money.Create(2_000), DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(Ct));
    }
}
