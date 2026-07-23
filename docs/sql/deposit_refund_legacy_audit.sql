/*
Dry-run audit for legacy booking deposit refund semantics.

Purpose:
- Find deposits that already have RefundedAmount / refunded statuses before the
  booking_deposit_refunds ledger became the source of refund workflow evidence.
- Classify records for manual finance review.

This script does not modify data.
Run against development/test first. Do not treat RefundedAmount alone as proof
that money was actually returned to the customer.
*/

SELECT
    d.booking_deposit_id AS BookingDepositId,
    b.booking_code AS BookingCode,
    d.paid_amount AS PaidAmount,
    d.applied_amount AS AppliedAmount,
    d.forfeited_amount AS ForfeitedAmount,
    d.refunded_amount AS RefundedAmount,
    d.status AS DepositStatus,
    d.refunded_at_utc AS RefundedAtUtc,
    d.applied_to_invoice_id AS AppliedToInvoiceId,
    CASE WHEN EXISTS (
        SELECT 1
        FROM payments p
        WHERE p.invoice_id = d.applied_to_invoice_id
          AND p.payment_status = 2
          AND p.note LIKE '%Booking deposit applied%'
    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasDepositApplicationPayment,
    CASE WHEN EXISTS (
        SELECT 1
        FROM booking_deposit_refunds r
        WHERE r.booking_deposit_id = d.booking_deposit_id
    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasRefundLedger,
    CASE WHEN EXISTS (
        SELECT 1
        FROM booking_deposit_refunds r
        WHERE r.booking_deposit_id = d.booking_deposit_id
          AND r.status = 6
          AND NULLIF(LTRIM(RTRIM(r.manual_transfer_code)), '') IS NOT NULL
    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasSucceededRefundTransferCode,
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM booking_deposit_refunds r
            WHERE r.booking_deposit_id = d.booking_deposit_id
              AND r.status = 6
              AND r.amount = d.refunded_amount
        ) THEN 'AlreadyMigrated'
        WHEN d.refunded_amount > 0
             AND EXISTS (
                SELECT 1
                FROM booking_deposit_refunds r
                WHERE r.booking_deposit_id = d.booking_deposit_id
                  AND r.status = 6
                  AND NULLIF(LTRIM(RTRIM(r.manual_transfer_code)), '') IS NOT NULL
             ) THEN 'ConfirmedSucceeded'
        WHEN d.refunded_amount > 0
             OR d.status IN (5, 6)
             OR d.refunded_at_utc IS NOT NULL THEN 'PotentiallyNotActuallyRefunded'
        ELSE 'ManualReview'
    END AS SuggestedClassification
FROM booking_deposits d
INNER JOIN bookings b ON b.booking_id = d.booking_id
WHERE d.refunded_amount > 0
   OR d.status IN (5, 6)
   OR d.refunded_at_utc IS NOT NULL
ORDER BY d.refunded_at_utc DESC, d.booking_deposit_id DESC;
