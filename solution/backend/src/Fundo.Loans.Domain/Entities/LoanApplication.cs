using Fundo.Loans.Domain.ValueObjects;

namespace Fundo.Loans.Domain.Entities;

/// <summary>
/// A loan application. The brief's deny path never reaches this entity — only approved
/// applications are ever constructed and persisted (see <c>SubmitLoanApplicationHandler</c>).
/// </summary>
public sealed class LoanApplication
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Money RequestedAmount { get; private set; }
    public LoanApplicationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private LoanApplication(Guid id, Guid customerId, Money requestedAmount, DateTime nowUtc)
    {
        Id = id;
        CustomerId = customerId;
        RequestedAmount = requestedAmount;
        Status = LoanApplicationStatus.Approved;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

#pragma warning disable CS8618 // EF Core materialization constructor.
    private LoanApplication()
    {
    }
#pragma warning restore CS8618

    public static LoanApplication Open(Guid customerId, Money requestedAmount, DateTime nowUtc)
        => new(Guid.CreateVersion7(), customerId, requestedAmount, nowUtc);

    public void ChangeRequestedAmount(Money requestedAmount, DateTime nowUtc)
    {
        RequestedAmount = requestedAmount;
        UpdatedAtUtc = nowUtc;
    }
}
