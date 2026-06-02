using PoolHub.Core.Entities;

namespace PoolHub.Core.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetInvoiceBySessionIdAsync(int sessionId, CancellationToken ct);
    Task AddInvoiceAsync(Invoice invoice, CancellationToken ct);
    Task AddPaymentAsync(Payment payment, CancellationToken ct);
    
    // Audit logs
    IQueryable<AuditLog> GetAuditLogs();

    Task<int> SaveChangesAsync(CancellationToken ct);
}
