using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Loans.Infrastructure.Persistence.Repositories;

public sealed class LoanApplicationRepository : ILoanApplicationRepository
{
    private readonly LoansDbContext _dbContext;

    public LoanApplicationRepository(LoansDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
        => _dbContext.LoanApplications.FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);

    public void Add(LoanApplication application) => _dbContext.LoanApplications.Add(application);
}
