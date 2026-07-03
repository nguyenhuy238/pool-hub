namespace PoolHub.Core.DTOs.Booking;

public class CancelBookingRequest
{
    public string? Reason { get; set; }
    public bool CancelledByVenue { get; set; }
}

public class NoShowBookingRequest
{
    public string? Reason { get; set; }
}

public class ConfirmDepositRequest
{
    public decimal PaidAmount { get; set; }
    public string? TransactionCode { get; set; }
}

public class RejectDepositTransferRequest
{
    public string? Reason { get; set; }
}
