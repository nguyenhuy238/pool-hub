using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class InvoiceRepository(PoolHubDbContext db) : IInvoiceRepository
{
    public async Task<Invoice?> GetInvoiceBySessionIdAsync(int sessionId, CancellationToken ct) 
        => await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);
    
    public async Task AddInvoiceAsync(Invoice invoice, CancellationToken ct) 
        => await db.Invoices.AddAsync(invoice, ct);
    
    public async Task AddPaymentAsync(Payment payment, CancellationToken ct) 
        => await db.Payments.AddAsync(payment, ct);

    public IQueryable<AuditLog> GetAuditLogs() 
        => db.AuditLogs.AsQueryable();

    public async Task<int> SaveChangesAsync(CancellationToken ct) 
        => await db.SaveChangesAsync(ct);
}
