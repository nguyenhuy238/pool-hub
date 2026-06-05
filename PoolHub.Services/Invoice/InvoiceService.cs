using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Exceptions;
using EntityInvoice = PoolHub.Core.Entities.Invoice;

namespace PoolHub.Services.Invoice;

public class InvoiceService(PoolHubDbContext db) : IInvoiceService
{
    public async Task<InvoiceDto> GenerateFromSessionAsync(long sessionId, long? issuedByUserId, CancellationToken ct)
    {
        var exists = await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (exists is not null) return new InvoiceDto { InvoiceId = exists.InvoiceId, SessionId = exists.SessionId, InvoiceCode = exists.InvoiceCode, GrandTotalAmount = exists.GrandTotalAmount };

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        var productTotal = await db.Orders.Where(o => o.SessionId == sessionId).SumAsync(o => o.SubtotalAmount, ct);
        var timeTotal = await db.SessionTableAssignments.Where(x => x.SessionId == sessionId).SumAsync(x => x.Amount ?? 0, ct);
        var subtotal = productTotal + timeTotal;

        var invoice = new EntityInvoice
        {
            SessionId = sessionId,
            CustomerId = session.CustomerId,
            InvoiceCode = $"INV{DateTime.UtcNow:yyyyMMddHHmmss}",
            TimeSubtotalAmount = timeTotal,
            ProductSubtotalAmount = productTotal,
            SubtotalAmount = subtotal,
            DiscountAmount = 0,
            TaxAmount = 0,
            GrandTotalAmount = subtotal,
            PaidAmount = 0,
            PaymentStatus = 1,
            Status = 1,
            IssuedByUserId = issuedByUserId,
            IssuedAtUtc = DateTime.UtcNow
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return new InvoiceDto { InvoiceId = invoice.InvoiceId, SessionId = invoice.SessionId, InvoiceCode = invoice.InvoiceCode, GrandTotalAmount = invoice.GrandTotalAmount };
    }

    public async Task CreatePaymentAsync(CreatePaymentRequest request, long? receivedByUserId, CancellationToken ct)
    {
        var payment = new Payment { InvoiceId = request.InvoiceId, PaymentMethodId = request.PaymentMethodId, Amount = request.Amount, PaymentStatus = 1, ReceivedByUserId = receivedByUserId, PaidAtUtc = DateTime.UtcNow };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
    }
}
