using PoolHub.Core.DTOs.Landing;

namespace PoolHub.Core.DTOs.Booking;

public class BookingDto
{
    public long BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public long? TableId { get; set; }
    public List<long> TableIds { get; set; } = [];
    public long? TableTypeId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int Status { get; set; }
    public int NumberOfGuests { get; set; }
    public string? Note { get; set; }
    public bool HasSession { get; set; }
    public decimal EstimatedAmount { get; set; }
    public bool RequiresApproval { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? HoldExpiresAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? NoShowAtUtc { get; set; }
    public string? Source { get; set; }
    public BookingDepositDto? Deposit { get; set; }
    public DepositPaymentInstructionDto? DepositPaymentInstruction { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string? DepositStatusText { get; set; }
}

public class BookingDepositDto
{
    public long BookingDepositId { get; set; }
    public decimal RequiredAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal ForfeitedAmount { get; set; }
    public int Status { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
