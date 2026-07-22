using PoolHub.Core.DTOs.CustomerReview;

namespace PoolHub.Core.DTOs.Invoice;

public class CreatePaymentResponse
{
    public long InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public int PaymentStatus { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal GrandTotalAmount { get; set; }
    public ReviewInvitationLinkDto? ReviewInvitation { get; set; }
}
