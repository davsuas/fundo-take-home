using Fundo.Loans.Domain.Entities;

namespace Fundo.Loans.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> FindBySsnHashAsync(string ssnHash, CancellationToken cancellationToken);

    void Add(Customer customer);
}
