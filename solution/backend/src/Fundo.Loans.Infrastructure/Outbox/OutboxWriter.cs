using Fundo.Loans.Application.Abstractions;
using Fundo.Loans.Domain.Entities;
using Fundo.Loans.Infrastructure.Persistence;

namespace Fundo.Loans.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly LoansDbContext _dbContext;

    public OutboxWriter(LoansDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Enqueue(OutboxMessage message) => _dbContext.OutboxMessages.Add(message);
}
