using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Admin;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Admin;
using PoolHub.Services.Audit;
using PoolHub.Shared.Constants;

namespace PoolHub.UnitTests;

public class ReportTimeTests
{
    [Fact]
    public async Task RevenueReport_GroupsByVietnamBusinessDateAtUtcBoundaries()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PoolHubDbContext(options);
        db.Invoices.AddRange(
            new PoolHub.Core.Entities.Invoice
            {
                InvoiceId = 1,
                InvoiceCode = "INV-BEFORE",
                PaymentStatus = InvoicePaymentStatuses.Paid,
                PaidAmount = 10,
                IssuedAtUtc = new DateTime(2026, 7, 9, 16, 59, 0, DateTimeKind.Utc)
            },
            new PoolHub.Core.Entities.Invoice
            {
                InvoiceId = 2,
                InvoiceCode = "INV-START",
                PaymentStatus = InvoicePaymentStatuses.Paid,
                PaidAmount = 20,
                IssuedAtUtc = new DateTime(2026, 7, 9, 17, 0, 0, DateTimeKind.Utc)
            },
            new PoolHub.Core.Entities.Invoice
            {
                InvoiceId = 3,
                InvoiceCode = "INV-END",
                PaymentStatus = InvoicePaymentStatuses.Paid,
                PaidAmount = 30,
                IssuedAtUtc = new DateTime(2026, 7, 10, 16, 59, 0, DateTimeKind.Utc)
            },
            new PoolHub.Core.Entities.Invoice
            {
                InvoiceId = 4,
                InvoiceCode = "INV-AFTER",
                PaymentStatus = InvoicePaymentStatuses.Paid,
                PaidAmount = 40,
                IssuedAtUtc = new DateTime(2026, 7, 10, 17, 0, 0, DateTimeKind.Utc)
            });
        await db.SaveChangesAsync();
        var service = new AdminManagementService(db, new AuditService(db, new HttpContextAccessor()));

        var rows = await service.GetRevenueReportAsync(new ReportQueryRequest
        {
            FromDate = new DateTime(2026, 7, 10),
            ToDate = new DateTime(2026, 7, 10)
        }, CancellationToken.None);

        var row = Assert.Single(rows);
        Assert.Equal(new DateTime(2026, 7, 10), row.Date);
        Assert.Equal(50, row.Revenue);
        Assert.Equal(2, row.InvoiceCount);
    }
}

