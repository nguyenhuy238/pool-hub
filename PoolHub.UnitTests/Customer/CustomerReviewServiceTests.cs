using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Audit;
using PoolHub.Core.DTOs.CustomerReview;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.CustomerReview;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;

namespace PoolHub.UnitTests;

public class CustomerReviewServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreatePublicReviewAsync_WhenRatingOutOfRange_ThrowsValidationException(int rating)
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreatePublicReviewAsync(new CreatePublicReviewRequest { Rating = rating }, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePublicReviewAsync_WhenSessionIsNotClosed_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        await SeedPaidInvoiceAsync(db, sessionEnded: false, invoicePaid: true);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreatePublicReviewAsync(new CreatePublicReviewRequest { ReferenceCode = "INV-1", Rating = 5 }, CancellationToken.None));

        Assert.Equal("Only closed sessions can be reviewed.", exception.Message);
    }

    [Fact]
    public async Task CreatePublicReviewAsync_WhenInvoiceIsNotPaid_ThrowsBusinessRuleException()
    {
        using var db = CreateDb();
        await SeedPaidInvoiceAsync(db, sessionEnded: true, invoicePaid: false);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreatePublicReviewAsync(new CreatePublicReviewRequest { ReferenceCode = "INV-1", Rating = 5 }, CancellationToken.None));

        Assert.Equal("Only paid invoices can be reviewed.", exception.Message);
    }

    [Fact]
    public async Task CreatePublicReviewAsync_WhenInvoiceIsPaidAndSessionClosed_CreatesVerifiedReview()
    {
        using var db = CreateDb();
        await SeedPaidInvoiceAsync(db, sessionEnded: true, invoicePaid: true);
        var service = CreateService(db);

        var result = await service.CreatePublicReviewAsync(new CreatePublicReviewRequest
        {
            ReferenceCode = "INV-1",
            Rating = 5,
            Content = "  Good table  "
        }, CancellationToken.None);

        Assert.Equal(5, result.Rating);
        Assert.Equal("Good table", result.Content);
        Assert.True(result.IsVerified);
    }

    [Fact]
    public async Task CreatePublicReviewAsync_WhenContentIsBlank_StoresNullAndReturnsEmptyContent()
    {
        using var db = CreateDb();
        await SeedPaidInvoiceAsync(db, sessionEnded: true, invoicePaid: true);
        var service = CreateService(db);

        var result = await service.CreatePublicReviewAsync(new CreatePublicReviewRequest
        {
            ReferenceCode = "INV-1",
            Rating = 4,
            Content = "   "
        }, CancellationToken.None);

        Assert.Equal(string.Empty, result.Content);
        Assert.Null(await db.CustomerReviews.Select(x => x.Content).SingleAsync());
    }

    [Fact]
    public async Task CreatePublicReviewAsync_WhenDuplicateInvoiceReview_ThrowsConflictException()
    {
        using var db = CreateDb();
        await SeedPaidInvoiceAsync(db, sessionEnded: true, invoicePaid: true);
        var service = CreateService(db);

        await service.CreatePublicReviewAsync(new CreatePublicReviewRequest { ReferenceCode = "INV-1", Rating = 5 }, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreatePublicReviewAsync(new CreatePublicReviewRequest { ReferenceCode = "INV-1", Rating = 4 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetPublicReviewsAsync_WhenAnonymous_DoesNotExposeRealName()
    {
        using var db = CreateDb();
        await SeedPaidInvoiceAsync(db, sessionEnded: true, invoicePaid: true);
        var service = CreateService(db);
        var created = await service.CreatePublicReviewAsync(new CreatePublicReviewRequest
        {
            ReferenceCode = "INV-1",
            Rating = 5,
            IsAnonymous = true
        }, CancellationToken.None);

        await service.ApproveAsync(created.PublicId, new ApproveCustomerReviewRequest(), 99, CancellationToken.None);

        var page = await service.GetPublicReviewsAsync(new PublicReviewQueryRequest(), CancellationToken.None);
        var review = Assert.Single(page.Items);
        Assert.Equal("Khách hàng ẩn danh", review.DisplayName);
        Assert.True(review.IsVerified);
    }

    [Fact]
    public async Task CreatePublicReviewAsync_WithoutTransactionContext_IsNotVerified()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.CreatePublicReviewAsync(new CreatePublicReviewRequest
        {
            Rating = 5,
            FullName = "Nguyen Quoc Huy"
        }, CancellationToken.None);

        Assert.False(result.IsVerified);
    }

    private static CustomerReviewService CreateService(PoolHubDbContext db) => new(db, new NoOpAuditService());

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static async Task SeedPaidInvoiceAsync(PoolHubDbContext db, bool sessionEnded, bool invoicePaid)
    {
        var startedAt = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Nguyen Quoc Huy", PhoneNumber = "0900000000", Status = true });
        db.Sessions.Add(new Session
        {
            SessionId = 1,
            SessionCode = "SS-1",
            CustomerId = 1,
            Status = sessionEnded ? 2 : 1,
            StartedAtUtc = startedAt,
            EndedAtUtc = sessionEnded ? startedAt.AddHours(2) : null,
            OpenedByUserId = 99
        });
        db.Invoices.Add(new Invoice
        {
            InvoiceId = 1,
            InvoiceCode = "INV-1",
            SessionId = 1,
            CustomerId = 1,
            GrandTotalAmount = 100000,
            PaidAmount = invoicePaid ? 100000 : 0,
            PaymentStatus = invoicePaid ? InvoicePaymentStatuses.Paid : InvoicePaymentStatuses.Unpaid,
            Status = invoicePaid ? 2 : 1
        });
        await db.SaveChangesAsync();
    }

    private sealed class NoOpAuditService : IAuditService
    {
        public Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryRequest request, CancellationToken ct) =>
            Task.FromResult(new PagedResult<AuditLogDto>());

        public Task<AuditLogDto> GetAuditLogAsync(long id, CancellationToken ct) =>
            Task.FromResult(new AuditLogDto());

        public Task LogAsync(long? actorUserId, string action, string entityName, long? entityId = null, Guid? entityPublicId = null, object? oldValues = null, object? newValues = null, string? description = null, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
