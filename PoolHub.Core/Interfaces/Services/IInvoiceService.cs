using PoolHub.Core.DTOs.Invoice;

namespace PoolHub.Core.Interfaces.Services;

public interface IInvoiceService
{
    Task<InvoiceDto> GenerateFromSessionAsync(long sessionId, long? issuedByUserId, CancellationToken ct);
    Task CreatePaymentAsync(CreatePaymentRequest request, long? receivedByUserId, CancellationToken ct);
}
