using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.CustomerReview;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using EntityBooking = PoolHub.Core.Entities.Booking;
using EntityCustomer = PoolHub.Core.Entities.Customer;
using EntityCustomerReviewInvitation = PoolHub.Core.Entities.CustomerReviewInvitation;
using EntityCustomerReview = PoolHub.Core.Entities.CustomerReview;
using EntityInvoice = PoolHub.Core.Entities.Invoice;
using EntitySession = PoolHub.Core.Entities.Session;

namespace PoolHub.Services.CustomerReview;

public class CustomerReviewService(PoolHubDbContext db, IAuditService auditService, IConfiguration? config = null) : ICustomerReviewService
{
    public async Task<PagedResult<PublicReviewDto>> GetPublicReviewsAsync(PublicReviewQueryRequest request, CancellationToken ct)
    {
        NormalizePagination(request);
        var query = db.CustomerReviews.AsNoTracking()
            .Where(x => x.Status == CustomerReviewStatuses.Approved);

        if (request.FeaturedOnly == true)
            query = query.Where(x => x.IsFeatured);
        if (request.MinRating.HasValue)
            query = query.Where(x => x.Rating >= request.MinRating.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.IsFeatured)
            .ThenBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.ApprovedAtUtc ?? x.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PublicReviewDto
            {
                PublicId = x.PublicId,
                Rating = x.Rating,
                Content = x.Content,
                DisplayName = x.DisplayName ?? "Khách hàng PoolHub",
                AvatarUrl = x.AvatarUrl,
                CheckInImageUrl = x.CheckInImageUrl,
                IsFeatured = x.IsFeatured,
                DisplayOrder = x.DisplayOrder,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct);

        return Page(items, request, total);
    }

    public async Task<CustomerReviewDto> CreatePublicReviewAsync(CreatePublicReviewRequest request, CancellationToken ct)
    {
        ValidateRatingAndContent(request.Rating, request.Content);
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone)) throw new ValidationException("Phone number is required.");

        var context = await ResolveReviewContextAsync(request.BookingCode, request.SessionCode, request.InvoiceCode, phone, ct);
        var customer = context.CustomerId.HasValue
            ? await db.Customers.FirstOrDefaultAsync(x => x.CustomerId == context.CustomerId.Value, ct)
            : await db.Customers.FirstOrDefaultAsync(x => x.PhoneNumber == phone, ct);

        if (customer is null && !string.IsNullOrWhiteSpace(request.FullName))
        {
            customer = new EntityCustomer
            {
                FullName = request.FullName.Trim(),
                PhoneNumber = phone,
                Status = true
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync(ct);
        }

        if (customer is not null)
        {
            await EnsureNoOpenDuplicateAsync(customer.CustomerId, context.BookingId, context.SessionId, context.InvoiceId, null, ct);
        }

        var review = new EntityCustomerReview
        {
            CustomerId = customer?.CustomerId,
            BookingId = context.BookingId,
            SessionId = context.SessionId,
            InvoiceId = context.InvoiceId,
            Rating = request.Rating,
            Content = request.Content.Trim(),
            DisplayName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim() : customer?.FullName,
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            CheckInImageUrl = NormalizeOptional(request.CheckInImageUrl),
            Status = CustomerReviewStatuses.Pending,
            Source = CustomerReviewSources.Public
        };

        db.CustomerReviews.Add(review);
        await db.SaveChangesAsync(ct);
        return await GetReviewAsync(review.PublicId, ct);
    }

    public async Task<PagedResult<CustomerReviewDto>> GetReviewsAsync(CustomerReviewQueryRequest request, CancellationToken ct)
    {
        NormalizePagination(request);
        var reviewSource = db.CustomerReviews.AsNoTracking();

        if (request.Status.HasValue) reviewSource = reviewSource.Where(x => x.Status == request.Status.Value);
        if (request.Rating.HasValue) reviewSource = reviewSource.Where(x => x.Rating == request.Rating.Value);
        if (request.CustomerId.HasValue) reviewSource = reviewSource.Where(x => x.CustomerId == request.CustomerId.Value);
        if (request.CustomerPublicId.HasValue)
        {
            reviewSource = reviewSource.Where(x =>
                x.CustomerId.HasValue &&
                db.Customers.AsNoTracking().Any(customer =>
                    customer.CustomerId == x.CustomerId.Value &&
                    customer.PublicId == request.CustomerPublicId.Value));
        }
        if (request.FromDate.HasValue) reviewSource = reviewSource.Where(x => x.CreatedAtUtc >= request.FromDate.Value);
        if (request.ToDate.HasValue) reviewSource = reviewSource.Where(x => x.CreatedAtUtc <= request.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            reviewSource = reviewSource.Where(x =>
                x.Content.Contains(search) ||
                (x.DisplayName != null && x.DisplayName.Contains(search)) ||
                (x.CustomerId.HasValue && db.Customers.AsNoTracking().Any(customer =>
                    customer.CustomerId == x.CustomerId.Value &&
                    (customer.FullName.Contains(search) || customer.PhoneNumber.Contains(search)))));
        }

        var total = await reviewSource.CountAsync(ct);
        var pageSource = reviewSource
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        var rows = await BaseReviewQuery(pageSource)
            .ToListAsync(ct);

        return Page(rows.Select(Map).ToList(), request, total);
    }

    public async Task<CustomerReviewDto> GetReviewAsync(Guid publicId, CancellationToken ct)
    {
        var reviewSource = db.CustomerReviews.AsNoTracking()
            .Where(review => review.PublicId == publicId);

        var row = await BaseReviewQuery(reviewSource).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Customer review not found.");
        return Map(row);
    }

    public async Task<CustomerReviewDto> ApproveAsync(Guid publicId, ApproveCustomerReviewRequest request, long actorUserId, CancellationToken ct)
    {
        var review = await GetEntityAsync(publicId, ct);
        if (review.CustomerId.HasValue)
            await EnsureNoOpenDuplicateAsync(review.CustomerId.Value, review.BookingId, review.SessionId, review.InvoiceId, review.CustomerReviewId, ct);

        var oldValues = Snapshot(review);
        review.Status = CustomerReviewStatuses.Approved;
        review.IsFeatured = request.IsFeatured ?? review.IsFeatured;
        review.DisplayOrder = request.DisplayOrder ?? review.DisplayOrder;
        review.ApprovedByUserId = actorUserId;
        review.ApprovedAtUtc = DateTime.UtcNow;
        review.RejectedReason = null;
        review.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(actorUserId, AuditActions.CustomerReviewApproved, review, oldValues, ct);
        return await GetReviewAsync(publicId, ct);
    }

    public async Task<CustomerReviewDto> RejectAsync(Guid publicId, RejectCustomerReviewRequest request, long actorUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("Reject reason is required.");
        var review = await GetEntityAsync(publicId, ct);
        var oldValues = Snapshot(review);
        review.Status = CustomerReviewStatuses.Rejected;
        review.IsFeatured = false;
        review.RejectedReason = request.Reason.Trim();
        review.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(actorUserId, AuditActions.CustomerReviewRejected, review, oldValues, ct);
        return await GetReviewAsync(publicId, ct);
    }

    public async Task<CustomerReviewDto> UpdateVisibilityAsync(Guid publicId, UpdateCustomerReviewVisibilityRequest request, long actorUserId, CancellationToken ct)
    {
        if (request.Status is not (CustomerReviewStatuses.Approved or CustomerReviewStatuses.Hidden))
            throw new ValidationException("Visibility status must be Approved or Hidden.");

        var review = await GetEntityAsync(publicId, ct);
        var oldValues = Snapshot(review);
        review.Status = request.Status;
        review.IsFeatured = request.Status == CustomerReviewStatuses.Approved && (request.IsFeatured ?? review.IsFeatured);
        review.DisplayOrder = request.DisplayOrder ?? review.DisplayOrder;
        if (request.Status == CustomerReviewStatuses.Approved && review.ApprovedAtUtc is null)
        {
            review.ApprovedByUserId = actorUserId;
            review.ApprovedAtUtc = DateTime.UtcNow;
        }
        review.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(actorUserId, request.Status == CustomerReviewStatuses.Hidden ? AuditActions.CustomerReviewHidden : AuditActions.CustomerReviewUpdated, review, oldValues, ct);
        return await GetReviewAsync(publicId, ct);
    }

    public async Task<CustomerReviewDto> UpdateAsync(Guid publicId, UpdateCustomerReviewRequest request, long actorUserId, CancellationToken ct)
    {
        ValidateRatingAndContent(1, request.Content);
        var review = await GetEntityAsync(publicId, ct);
        var oldValues = Snapshot(review);
        review.Content = request.Content.Trim();
        review.DisplayName = NormalizeOptional(request.DisplayName);
        review.AvatarUrl = NormalizeOptional(request.AvatarUrl);
        review.CheckInImageUrl = NormalizeOptional(request.CheckInImageUrl);
        review.IsFeatured = request.IsFeatured;
        review.DisplayOrder = request.DisplayOrder;
        review.Note = NormalizeOptional(request.Note);
        review.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(actorUserId, AuditActions.CustomerReviewUpdated, review, oldValues, ct);
        return await GetReviewAsync(publicId, ct);
    }

    public async Task HideAsync(Guid publicId, long actorUserId, CancellationToken ct)
    {
        var review = await GetEntityAsync(publicId, ct);
        var oldValues = Snapshot(review);
        review.Status = CustomerReviewStatuses.Hidden;
        review.IsFeatured = false;
        review.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await LogAsync(actorUserId, AuditActions.CustomerReviewHidden, review, oldValues, ct);
    }

    public async Task<ReviewInvitationLinkDto> CreateInvitationForInvoiceAsync(long invoiceId, long? actorUserId, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.InvoiceId == invoiceId, ct)
            ?? throw new NotFoundException("Invoice not found.");
        if (invoice.PaymentStatus != InvoicePaymentStatuses.Paid)
            throw new BusinessRuleException("Review invitation can only be created for paid invoices.");

        var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionId == invoice.SessionId, ct)
            ?? throw new NotFoundException("Session not found.");
        if (session.EndedAtUtc is null)
            throw new BusinessRuleException("Review invitation can only be created after the session is closed.");

        if (await HasExistingReviewAsync(invoice.CustomerId, null, session.SessionId, invoice.InvoiceId, ct))
            throw new ConflictException("This invoice/session already has a pending or approved review.");

        var oldActiveInvitations = await db.CustomerReviewInvitations
            .Where(x => x.InvoiceId == invoiceId && x.Status == CustomerReviewInvitationStatuses.Active && x.UsedAtUtc == null)
            .ToListAsync(ct);
        foreach (var oldInvitation in oldActiveInvitations)
        {
            oldInvitation.Status = CustomerReviewInvitationStatuses.Revoked;
            oldInvitation.UpdatedAtUtc = DateTime.UtcNow;
        }

        var token = GenerateToken();
        var invitation = new EntityCustomerReviewInvitation
        {
            TokenHash = HashToken(token),
            CustomerId = invoice.CustomerId,
            SessionId = invoice.SessionId,
            InvoiceId = invoice.InvoiceId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(GetInvitationExpiryDays()),
            CreatedByUserId = actorUserId,
            Status = CustomerReviewInvitationStatuses.Active
        };

        db.CustomerReviewInvitations.Add(invitation);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.CustomerReviewInvitationCreated, nameof(EntityCustomerReviewInvitation),
            invitation.CustomerReviewInvitationId, invitation.PublicId,
            newValues: new { invitation.InvoiceId, invitation.SessionId, invitation.CustomerId, invitation.ExpiresAtUtc },
            description: "Customer review invitation created.", ct: ct);

        return new ReviewInvitationLinkDto
        {
            PublicId = invitation.PublicId,
            ReviewUrl = BuildReviewUrl(token),
            ExpiresAtUtc = invitation.ExpiresAtUtc
        };
    }

    public async Task<ReviewInvitationDto> GetInvitationAsync(string token, CancellationToken ct)
    {
        var row = await GetInvitationProjectionAsync(token, ct);
        return MapInvitation(row);
    }

    public async Task<CustomerReviewDto> SubmitInvitationAsync(string token, SubmitReviewInvitationRequest request, CancellationToken ct)
    {
        ValidateRatingAndContent(request.Rating, request.Content);
        var row = await GetInvitationProjectionAsync(token, ct);
        var invitation = row.Invitation;
        var mapped = MapInvitation(row);
        if (!mapped.CanSubmit) throw new BusinessRuleException(mapped.Reason ?? "Review invitation cannot be used.");

        if (await HasExistingReviewAsync(invitation.CustomerId, null, invitation.SessionId, invitation.InvoiceId, ct))
            throw new ConflictException("This invoice/session already has a pending or approved review.");

        var review = new EntityCustomerReview
        {
            CustomerId = invitation.CustomerId,
            SessionId = invitation.SessionId,
            InvoiceId = invitation.InvoiceId,
            Rating = request.Rating,
            Content = request.Content.Trim(),
            DisplayName = NormalizeOptional(request.DisplayName) ?? row.Customer?.FullName,
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            CheckInImageUrl = NormalizeOptional(request.CheckInImageUrl),
            Status = CustomerReviewStatuses.Pending,
            Source = CustomerReviewSources.Public
        };

        invitation.Status = CustomerReviewInvitationStatuses.Used;
        invitation.UsedAtUtc = DateTime.UtcNow;
        invitation.UpdatedAtUtc = DateTime.UtcNow;
        db.CustomerReviews.Add(review);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(null, AuditActions.CustomerReviewInvitationUsed, nameof(EntityCustomerReviewInvitation),
            invitation.CustomerReviewInvitationId, invitation.PublicId,
            newValues: new { invitation.InvoiceId, invitation.SessionId, invitation.CustomerId, invitation.UsedAtUtc },
            description: "Customer review invitation used.", ct: ct);

        return await GetReviewAsync(review.PublicId, ct);
    }

    private async Task<ReviewContext> ResolveReviewContextAsync(string? bookingCode, string? sessionCode, string? invoiceCode, string phone, CancellationToken ct)
    {
        var provided = new[] { bookingCode, sessionCode, invoiceCode }.Count(x => !string.IsNullOrWhiteSpace(x));
        if (provided == 0) return new ReviewContext(null, null, null, null);
        if (provided > 1) throw new ValidationException("Only one of bookingCode, sessionCode, or invoiceCode can be provided.");

        if (!string.IsNullOrWhiteSpace(bookingCode))
        {
            var row = await db.Bookings.AsNoTracking()
                .Where(x => x.BookingCode == bookingCode.Trim())
                .Select(x => new { x.BookingId, x.CustomerId, x.Status, CustomerPhone = db.Customers.Where(c => c.CustomerId == x.CustomerId).Select(c => c.PhoneNumber).FirstOrDefault() })
                .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Booking code not found.");
            EnsurePhoneMatches(phone, row.CustomerPhone);
            if (row.Status != BookingStatuses.Completed)
                throw new BusinessRuleException("Only completed bookings can be reviewed.");
            return new ReviewContext(row.CustomerId, row.BookingId, null, null);
        }

        if (!string.IsNullOrWhiteSpace(sessionCode))
        {
            var row = await db.Sessions.AsNoTracking()
                .Where(x => x.SessionCode == sessionCode.Trim())
                .Select(x => new { x.SessionId, x.CustomerId, x.EndedAtUtc, CustomerPhone = x.CustomerId.HasValue ? db.Customers.Where(c => c.CustomerId == x.CustomerId.Value).Select(c => c.PhoneNumber).FirstOrDefault() : null })
                .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Session code not found.");
            EnsurePhoneMatches(phone, row.CustomerPhone);
            if (row.EndedAtUtc is null)
                throw new BusinessRuleException("Only closed sessions can be reviewed.");
            return new ReviewContext(row.CustomerId, null, row.SessionId, null);
        }

        var invoice = await db.Invoices.AsNoTracking()
            .Where(x => x.InvoiceCode == invoiceCode!.Trim())
            .Select(x => new { x.InvoiceId, x.CustomerId, x.SessionId, x.PaymentStatus, CustomerPhone = x.CustomerId.HasValue ? db.Customers.Where(c => c.CustomerId == x.CustomerId.Value).Select(c => c.PhoneNumber).FirstOrDefault() : null })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Invoice code not found.");
        EnsurePhoneMatches(phone, invoice.CustomerPhone);
        if (invoice.PaymentStatus != InvoicePaymentStatuses.Paid)
            throw new BusinessRuleException("Only paid invoices can be reviewed.");
        return new ReviewContext(invoice.CustomerId, null, invoice.SessionId, invoice.InvoiceId);
    }

    private async Task EnsureNoOpenDuplicateAsync(long customerId, long? bookingId, long? sessionId, long? invoiceId, long? currentReviewId, CancellationToken ct)
    {
        if (bookingId is null && sessionId is null && invoiceId is null) return;

        var exists = await db.CustomerReviews.AnyAsync(x =>
            x.CustomerReviewId != currentReviewId &&
            x.CustomerId == customerId &&
            (x.Status == CustomerReviewStatuses.Pending || x.Status == CustomerReviewStatuses.Approved) &&
            ((bookingId.HasValue && x.BookingId == bookingId.Value) ||
             (sessionId.HasValue && x.SessionId == sessionId.Value) ||
             (invoiceId.HasValue && x.InvoiceId == invoiceId.Value)), ct);

        if (exists) throw new ConflictException("This customer already has a pending or approved review for the same booking/session/invoice.");
    }

    private Task<bool> HasExistingReviewAsync(long? customerId, long? bookingId, long? sessionId, long? invoiceId, CancellationToken ct)
    {
        if (customerId is null && bookingId is null && sessionId is null && invoiceId is null) return Task.FromResult(false);

        return db.CustomerReviews.AnyAsync(x =>
            (x.Status == CustomerReviewStatuses.Pending || x.Status == CustomerReviewStatuses.Approved) &&
            (!customerId.HasValue || x.CustomerId == customerId.Value) &&
            ((bookingId.HasValue && x.BookingId == bookingId.Value) ||
             (sessionId.HasValue && x.SessionId == sessionId.Value) ||
             (invoiceId.HasValue && x.InvoiceId == invoiceId.Value)), ct);
    }

    private IQueryable<ReviewProjection> BaseReviewQuery(IQueryable<EntityCustomerReview> reviewSource) =>
        from review in reviewSource
        join customer in db.Customers.AsNoTracking() on review.CustomerId equals customer.CustomerId into customers
        from customer in customers.DefaultIfEmpty()
        join booking in db.Bookings.AsNoTracking() on review.BookingId equals booking.BookingId into bookings
        from booking in bookings.DefaultIfEmpty()
        join session in db.Sessions.AsNoTracking() on review.SessionId equals session.SessionId into sessions
        from session in sessions.DefaultIfEmpty()
        join invoice in db.Invoices.AsNoTracking() on review.InvoiceId equals invoice.InvoiceId into invoices
        from invoice in invoices.DefaultIfEmpty()
        select new ReviewProjection(review, customer, booking, session, invoice);

    private async Task<EntityCustomerReview> GetEntityAsync(Guid publicId, CancellationToken ct) =>
        await db.CustomerReviews.FirstOrDefaultAsync(x => x.PublicId == publicId, ct)
        ?? throw new NotFoundException("Customer review not found.");

    private async Task<InvitationProjection> GetInvitationProjectionAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new NotFoundException("Review invitation not found.");
        var tokenHash = HashToken(token.Trim());

        var row = await (from invitation in db.CustomerReviewInvitations
                         join invoice in db.Invoices.AsNoTracking() on invitation.InvoiceId equals invoice.InvoiceId
                         join session in db.Sessions.AsNoTracking() on invitation.SessionId equals session.SessionId
                         join customer in db.Customers.AsNoTracking() on invitation.CustomerId equals customer.CustomerId into customers
                         from customer in customers.DefaultIfEmpty()
                         where invitation.TokenHash == tokenHash
                         select new InvitationProjection(
                             invitation,
                             invoice,
                             session,
                             customer,
                             (from assignment in db.SessionTableAssignments.AsNoTracking()
                              join table in db.VenueTables.AsNoTracking() on assignment.TableId equals table.TableId
                              where assignment.SessionId == session.SessionId
                              orderby assignment.SessionTableAssignmentId
                              select table.TableName).FirstOrDefault()))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Review invitation not found.");

        if (row.Invitation.Status == CustomerReviewInvitationStatuses.Active && row.Invitation.ExpiresAtUtc <= DateTime.UtcNow)
        {
            row.Invitation.Status = CustomerReviewInvitationStatuses.Expired;
            row.Invitation.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return row;
    }

    private ReviewInvitationDto MapInvitation(InvitationProjection row)
    {
        var reason = GetInvitationBlockReason(row);
        return new ReviewInvitationDto
        {
            PublicId = row.Invitation.PublicId,
            CustomerDisplayName = row.Customer?.FullName ?? "Khách hàng PoolHub",
            InvoiceCode = row.Invoice.InvoiceCode,
            SessionCode = row.Session.SessionCode,
            PlayedAt = row.Session.EndedAtUtc ?? row.Session.StartedAtUtc,
            TableName = row.TableName,
            CanSubmit = reason is null,
            Reason = reason
        };
    }

    private static string? GetInvitationBlockReason(InvitationProjection row)
    {
        if (row.Invitation.Status == CustomerReviewInvitationStatuses.Used || row.Invitation.UsedAtUtc.HasValue) return "Invitation already used.";
        if (row.Invitation.Status == CustomerReviewInvitationStatuses.Revoked) return "Invitation was revoked.";
        if (row.Invitation.Status == CustomerReviewInvitationStatuses.Expired || row.Invitation.ExpiresAtUtc <= DateTime.UtcNow) return "Invitation expired.";
        if (row.Invoice.PaymentStatus != InvoicePaymentStatuses.Paid) return "Invoice is not paid.";
        if (row.Session.EndedAtUtc is null) return "Session is not closed.";
        return null;
    }

    private static CustomerReviewDto Map(ReviewProjection row) => new()
    {
        PublicId = row.Review.PublicId,
        CustomerId = row.Review.CustomerId,
        CustomerPublicId = row.Customer?.PublicId,
        CustomerName = row.Customer?.FullName,
        PhoneNumber = row.Customer?.PhoneNumber,
        BookingCode = row.Booking?.BookingCode,
        SessionCode = row.Session?.SessionCode,
        InvoiceCode = row.Invoice?.InvoiceCode,
        Rating = row.Review.Rating,
        Content = row.Review.Content,
        DisplayName = row.Review.DisplayName ?? row.Customer?.FullName ?? "Khách hàng PoolHub",
        AvatarUrl = row.Review.AvatarUrl,
        CheckInImageUrl = row.Review.CheckInImageUrl,
        Status = row.Review.Status,
        IsFeatured = row.Review.IsFeatured,
        DisplayOrder = row.Review.DisplayOrder,
        Source = row.Review.Source,
        CreatedAtUtc = row.Review.CreatedAtUtc,
        UpdatedAtUtc = row.Review.UpdatedAtUtc,
        ApprovedAtUtc = row.Review.ApprovedAtUtc,
        RejectedReason = row.Review.RejectedReason,
        Note = row.Review.Note
    };

    private static void ValidateRatingAndContent(int rating, string? content)
    {
        if (rating is < 1 or > 5) throw new ValidationException("Rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(content)) throw new ValidationException("Review content is required.");
        if (content.Trim().Length > 1000) throw new ValidationException("Review content must not exceed 1000 characters.");
    }

    private static void EnsurePhoneMatches(string requestPhone, string? entityPhone)
    {
        if (!string.IsNullOrWhiteSpace(entityPhone) && PhoneNumberNormalizer.Normalize(entityPhone) != requestPhone)
            throw new BusinessRuleException("Phone number does not match the referenced booking/session/invoice.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void NormalizePagination(PaginationRequest request)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
    }

    private static PagedResult<T> Page<T>(List<T> items, PaginationRequest request, int total) => new()
    {
        Items = items,
        PageNumber = request.PageNumber,
        PageSize = request.PageSize,
        TotalItems = total,
        TotalCount = total
    };

    private static object Snapshot(EntityCustomerReview review) => new
    {
        review.Status,
        review.Content,
        review.DisplayName,
        review.IsFeatured,
        review.DisplayOrder,
        review.RejectedReason,
        review.Note
    };

    private Task LogAsync(long actorUserId, string action, EntityCustomerReview review, object oldValues, CancellationToken ct) =>
        auditService.LogAsync(actorUserId, action, nameof(EntityCustomerReview), review.CustomerReviewId, review.PublicId,
            oldValues, Snapshot(review), "Customer review changed.", ct);

    private int GetInvitationExpiryDays() =>
        int.TryParse(config?["Reviews:InvitationExpiryDays"], out var days) ? Math.Clamp(days, 1, 90) : 30;

    private string BuildReviewUrl(string token)
    {
        var baseUrl = config?["EmailSettings:FrontendBaseUrl"] ?? "http://localhost:3000";
        return $"{baseUrl.TrimEnd('/')}/reviews/new?token={Uri.EscapeDataString(token)}";
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record ReviewContext(long? CustomerId, long? BookingId, long? SessionId, long? InvoiceId);
    private sealed record ReviewProjection(EntityCustomerReview Review, EntityCustomer? Customer, EntityBooking? Booking, EntitySession? Session, EntityInvoice? Invoice);
    private sealed record InvitationProjection(EntityCustomerReviewInvitation Invitation, EntityInvoice Invoice, EntitySession Session, EntityCustomer? Customer, string? TableName);
}
