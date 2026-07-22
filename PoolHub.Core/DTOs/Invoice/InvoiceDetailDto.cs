using System;
using System.Collections.Generic;

namespace PoolHub.Core.DTOs.Invoice;

public class InvoiceDetailDto
{
    public long InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public long SessionId { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal TimeSubtotalAmount { get; set; }
    public decimal ProductSubtotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DepositAppliedAmount { get; set; }
    public decimal DepositRefundAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int PaymentStatus { get; set; }
    public int Status { get; set; }
    public long? IssuedByUserId { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public string? Note { get; set; }
    public List<InvoiceLineDto> Lines { get; set; } = [];
    public List<InvoiceDiscountDto> Discounts { get; set; } = [];
    public List<PaymentDto> Payments { get; set; } = [];
}
