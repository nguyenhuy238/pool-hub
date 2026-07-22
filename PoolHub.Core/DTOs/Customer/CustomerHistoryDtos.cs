namespace PoolHub.Core.DTOs.Customer;

public class CustomerBookingHistoryDto
{
    public long BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public long? TableId { get; set; }
    public string? TableName { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int Status { get; set; }
}

public class CustomerSessionHistoryDto
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int Status { get; set; }
}

public class CustomerInvoiceHistoryDto
{
    public long InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public decimal GrandTotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int PaymentStatus { get; set; }
    public int Status { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
}
