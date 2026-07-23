using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers.Booking;

[ApiController]
[Route("api/deposit-refunds")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Cashier)]
public class DepositRefundsController(IBookingDepositRefundService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<DepositRefundManagementDto>>>> Get([FromQuery] DepositRefundQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<DepositRefundManagementDto>>.Ok(await service.GetRefundsAsync(request, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<DepositRefundManagementDto>>> GetById(long id, CancellationToken ct) =>
        Ok(ApiResponse<DepositRefundManagementDto>.Ok(await service.GetManagementAsync(id, ct)));

    [HttpGet("{id:long}/bank-info")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<DepositRefundBankInfoDto>>> GetBankInfo(long id, CancellationToken ct) =>
        Ok(ApiResponse<DepositRefundBankInfoDto>.Ok(await service.GetBankInfoAsync(id, User.GetUserId(), ct)));

    [HttpPost("{id:long}/approve")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<BookingDepositRefundDto>>> Approve(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDepositRefundDto>.Ok(await service.ApproveAsync(id, User.GetUserId(), ct), "Refund approved."));

    [HttpPost("{id:long}/reject")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<BookingDepositRefundDto>>> Reject(long id, [FromBody] RejectDepositRefundRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDepositRefundDto>.Ok(await service.RejectAsync(id, User.GetUserId(), request.Reason, ct), "Refund rejected."));

    [HttpPost("{id:long}/request-customer-update")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
    public async Task<ActionResult<ApiResponse<BookingDepositRefundDto>>> RequestCustomerUpdate(long id, [FromBody] RequestCustomerRefundUpdateRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDepositRefundDto>.Ok(await service.RequestCustomerUpdateAsync(id, User.GetUserId(), request.Reason, ct), "Customer update requested."));

    [HttpPost("{id:long}/mark-processing")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<BookingDepositRefundDto>>> MarkProcessing(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDepositRefundDto>.Ok(await service.MarkProcessingAsync(id, User.GetUserId(), ct), "Refund marked processing."));

    [HttpPost("{id:long}/complete-bank-transfer")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<BookingDepositRefundDto>>> CompleteBankTransfer(long id, [FromBody] CompleteBankTransferRefundRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDepositRefundDto>.Ok(await service.CompleteBankTransferAsync(id, User.GetUserId(), request, ct), "Bank transfer refund completed."));

    [HttpPost("{id:long}/mark-failed")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<BookingDepositRefundDto>>> MarkFailed(long id, [FromBody] MarkDepositRefundFailedRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDepositRefundDto>.Ok(await service.MarkFailedAsync(id, User.GetUserId(), request.Reason, ct), "Refund marked failed."));

    [HttpPost("{id:long}/prepare-cash-pickup")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<BookingDepositRefundDto>>> PrepareCashPickup(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDepositRefundDto>.Ok(await service.PrepareCashPickupAsync(id, User.GetUserId(), ct), "Cash pickup prepared."));

    [HttpPost("{id:long}/complete-cash-pickup")]
    [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Cashier)]
    public async Task<ActionResult<ApiResponse<BookingDepositRefundDto>>> CompleteCashPickup(long id, [FromBody] CompleteCashPickupRefundRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDepositRefundDto>.Ok(await service.CompleteCashPickupAsync(id, User.GetUserId(), request, ct), "Cash pickup completed."));
}
