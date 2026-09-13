using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Loans.Infrastructure.Persistence;

public sealed class EfSsnBlacklist : ISsnBlacklist
{
    private readonly LoansDbContext _dbContext;
    private readonly ISsnHasher _hasher;

    public EfSsnBlacklist(LoansDbContext dbContext, ISsnHasher hasher)
    {
        _dbContext = dbContext;
        _hasher = hasher;
    }

    public Task<bool> ContainsAsync(Ssn ssn, CancellationToken cancellationToken)
    {
        var hash = _hasher.Hash(ssn);
        return _dbContext.BlacklistedSsns.AnyAsync(b => b.SsnHash == hash, cancellationToken);
    }
}
