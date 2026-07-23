using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class BookingsController
{
    [HttpPost("{id:long}/deposit/submit-transfer")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BookingDto>>> SubmitDepositTransfer(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.SubmitDepositTransferAsync(id, ct), "Deposit transfer submitted"));

    [HttpPost("{id:long}/deposit/confirm")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> ConfirmDeposit(long id, [FromBody] ConfirmDepositRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.ConfirmDepositAsync(id, request, User.GetUserId(), ct), "Deposit confirmed"));

    [HttpPost("{id:long}/deposit/reject")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> RejectDepositTransfer(long id, [FromBody] RejectDepositTransferRequest request, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.RejectDepositTransferAsync(id, request, User.GetUserId(), ct), "Deposit transfer rejected"));

    [HttpPost("{id:long}/deposit/mock-pay")]
    [Authorize(Roles = RoleConstants.Operation)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> MockPayDeposit(long id, CancellationToken ct) =>
        Ok(ApiResponse<BookingDto>.Ok(await _bookingService.MockPayDepositAsync(id, ct), "Deposit paid"));
}
