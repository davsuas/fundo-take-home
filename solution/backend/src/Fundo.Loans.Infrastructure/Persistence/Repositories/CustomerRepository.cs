using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Loans.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly LoansDbContext _dbContext;

    public CustomerRepository(LoansDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Customer?> FindBySsnHashAsync(string ssnHash, CancellationToken cancellationToken)
        => _dbContext.Customers.FirstOrDefaultAsync(c => c.SsnHash == ssnHash, cancellationToken);

    public void Add(Customer customer) => _dbContext.Customers.Add(customer);
}
