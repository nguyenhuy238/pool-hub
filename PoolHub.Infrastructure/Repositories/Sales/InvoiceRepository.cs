using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class InvoiceRepository(PoolHubDbContext db) : IInvoiceRepository
{
    public Task<Invoice?> GetInvoiceWithLinesAsync(long invoiceId, CancellationToken ct) =>
        db.Invoices.FirstOrDefaultAsync(x => x.InvoiceId == invoiceId, ct);
}
