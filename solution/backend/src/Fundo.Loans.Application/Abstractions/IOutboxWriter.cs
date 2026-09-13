using Fundo.Loans.Domain.Entities;

namespace Fundo.Loans.Application.Abstractions;

/// <summary>Enqueues an outbox row inside the current unit of work. It is only ever durable if the surrounding transaction commits.</summary>
public interface IOutboxWriter
{
    void Enqueue(OutboxMessage message);
}
