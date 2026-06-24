namespace PoolHub.Shared.Constants;

public static class BookingStatuses
{
    public const int Pending = 1;
    public const int Confirmed = 2;
    public const int Cancelled = 3;
    public const int Completed = 4;
    public const int NoShow = 5;
}

public static class InvoicePaymentStatuses
{
    public const int Unpaid = 1;
    public const int PartiallyPaid = 2;
    public const int Paid = 3;
    public const int Refunded = 4;
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
