using PoolHub.Core.DTOs.Invoice;

namespace PoolHub.Core.Interfaces.Services;

public interface IInvoiceService
{
    Task<InvoiceDto> GenerateFromSessionAsync(int sessionId, CancellationToken ct);
    Task CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct);
}
