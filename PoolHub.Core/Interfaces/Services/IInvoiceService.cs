using PoolHub.Core.DTOs.Invoice;
using PoolHub.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace PoolHub.Core.Interfaces.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetInvoicesAsync(InvoiceQueryRequest request, CancellationToken ct);
    Task<InvoiceDto> GenerateFromSessionAsync(long sessionId, long? issuedByUserId, CancellationToken ct);
    Task CreatePaymentAsync(CreatePaymentRequest request, long? receivedByUserId, CancellationToken ct);
    Task<InvoiceDetailDto> GetInvoiceDetailAsync(long id, CancellationToken ct);
    Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(CancellationToken ct);
    Task ApplyDiscountAsync(long invoiceId, ApplyDiscountRequest request, long userId, CancellationToken ct);
}
