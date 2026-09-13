using Fundo.Loans.Domain.Entities;

namespace Fundo.Loans.Application.Abstractions;

public interface ILoanApplicationRepository
{
    Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    void Add(LoanApplication application);
}
