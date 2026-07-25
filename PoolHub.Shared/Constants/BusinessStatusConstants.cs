namespace PoolHub.Shared.Constants;

public static class BookingStatuses
{
    public const int Pending = 1;
    public const int Confirmed = 2;
    public const int Cancelled = 3;
    public const int Completed = 4;
    public const int NoShow = 5;
    public const int PendingDeposit = 6;
    public const int PendingApproval = 7;
    public const int Expired = 8;
    public const int InProgress = 9;
}

public static class TableOperationalStatuses
{
    public const int Available = 1;
    public const int InUse = 2;
    public const int Reserved = 3;
    public const int Maintenance = 4;
    public const int Inactive = 5;
}

public static class BookingDepositStatuses
{
    public const int NotRequired = 1;
    public const int Pending = 2;
    public const int Paid = 3;
    public const int AppliedToInvoice = 4;
    public const int Refunded = 5;
    public const int PartiallyRefunded = 6;
    public const int Forfeited = 7;
    public const int Expired = 8;
    public const int PendingVerification = 9;
}

public static class BookingDepositRefundStatuses
{
    public const int PendingCustomerInfo = 1;
    public const int PendingApproval = 2;
    public const int Approved = 3;
    public const int Processing = 4;
    public const int ReadyForCashPickup = 5;
    public const int Succeeded = 6;
    public const int Failed = 7;
    public const int Rejected = 8;
    public const int Cancelled = 9;
}

public static class BookingDepositRefundMethods
{
    public const int BankTransfer = 1;
    public const int CashAtVenue = 2;
}

public static class BookingDepositRefundReasons
{
    public const string CustomerCancelledInTime = "CustomerCancelledInTime";
    public const string CustomerCancelledLate = "CustomerCancelledLate";
    public const string VenueFault = "VenueFault";
    public const string BookingRejected = "BookingRejected";
    public const string DuplicateDeposit = "DuplicateDeposit";
    public const string DepositExcess = "DepositExcess";
    public const string ManualAdjustment = "ManualAdjustment";
    public const string Other = "Other";
}

public static class InvoicePaymentStatuses
{
    public const int Unpaid = 1;
    public const int PartiallyPaid = 2;
    public const int Paid = 3;
    public const int Refunded = 4;
}

public static class CustomerReviewStatuses
{
    public const int Pending = 1;
    public const int Approved = 2;
    public const int Rejected = 3;
    public const int Hidden = 4;
}

public static class CustomerReviewSources
{
    public const string Public = "Public";
    public const string Staff = "Staff";
    public const string AdminImport = "AdminImport";
}

public static class CustomerReviewInvitationStatuses
{
    public const int Active = 1;
    public const int Used = 2;
    public const int Expired = 3;
    public const int Revoked = 4;
}

public static class PaymentStatuses
{
    public const int Pending = 1;
    public const int Completed = 2;
    public const int Failed = 3;
    public const int Refunded = 4;
}

public static class InventoryTransactionTypes
{
    public const int Import = 1;
    public const int Export = 2;
    public const int Adjustment = 3;
    public const int Sale = 4;
    public const int CancelSale = 5;
}

public static class DiscountTypes
{
    public const string Percentage = "PERCENTAGE";
    public const string FixedAmount = "FIXED_AMOUNT";
}
