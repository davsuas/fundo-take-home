using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Domain.ValueObjects;
using Xunit;

namespace Fundo.Loans.Domain.UnitTests.Entities;

public class LoanApplicationTests
{
    [Fact]
    public void Open_CreatesApprovedApplication()
    {
        var now = DateTime.UtcNow;
        var customerId = Guid.CreateVersion7();
        var amount = Money.Create(10_000);

        var application = LoanApplication.Open(customerId, amount, now);

        Assert.Equal(customerId, application.CustomerId);
        Assert.Equal(LoanApplicationStatus.Approved, application.Status);
        Assert.Equal(amount, application.RequestedAmount);
    }

    [Fact]
    public void ChangeRequestedAmount_UpdatesAmountAndUpdatedAt()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var application = LoanApplication.Open(Guid.CreateVersion7(), Money.Create(10_000), created);

        var changedAt = created.AddDays(1);
        var newAmount = Money.Create(20_000);
        application.ChangeRequestedAmount(newAmount, changedAt);

        Assert.Equal(newAmount, application.RequestedAmount);
        Assert.Equal(changedAt, application.UpdatedAtUtc);
        Assert.Equal(created, application.CreatedAtUtc);
    }
}
