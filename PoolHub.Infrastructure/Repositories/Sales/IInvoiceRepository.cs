using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetInvoiceWithLinesAsync(long invoiceId, CancellationToken ct);
}
