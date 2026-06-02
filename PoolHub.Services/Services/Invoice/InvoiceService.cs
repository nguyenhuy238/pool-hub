using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;

namespace PoolHub.Services.Services.Invoice;

public class InvoiceService(IInvoiceRepository invoiceRepo, IOrderRepository orderRepo) : IInvoiceService
{
    public async Task<InvoiceDto> GenerateFromSessionAsync(int sessionId, CancellationToken ct)
    {
        var exists = await invoiceRepo.GetInvoiceBySessionIdAsync(sessionId, ct);
        if (exists is not null) return new InvoiceDto { InvoiceId = exists.InvoiceId, SessionId = exists.SessionId, InvoiceCode = exists.InvoiceCode, FinalAmount = exists.FinalAmount };

        var total = await orderRepo.GetTotalLineAmountBySessionAsync(sessionId, ct);

        var invoice = new PoolHub.Core.Entities.Invoice { SessionId = sessionId, InvoiceCode = $"INV{DateTime.UtcNow:yyyyMMddHHmmss}", SubtotalAmount = total, DiscountAmount = 0, FinalAmount = total, Status = 1 };
        await invoiceRepo.AddInvoiceAsync(invoice, ct);
        await invoiceRepo.SaveChangesAsync(ct);
        return new InvoiceDto { InvoiceId = invoice.InvoiceId, SessionId = invoice.SessionId, InvoiceCode = invoice.InvoiceCode, FinalAmount = invoice.FinalAmount };
    }

    public async Task CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct)
    {
        var payment = new Payment { InvoiceId = request.InvoiceId, PaymentMethodId = request.PaymentMethodId, Amount = request.Amount, Status = 1 };
        await invoiceRepo.AddPaymentAsync(payment, ct);
        await invoiceRepo.SaveChangesAsync(ct);
    }
}


