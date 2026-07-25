using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.API.Controllers.Booking;

[ApiController]
[Route("api/public/deposit-refunds")]
[AllowAnonymous]
[EnableRateLimiting("PublicDepositRefund")]
public class PublicDepositRefundsController(IBookingDepositRefundService service) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<ActionResult<ApiResponse<PublicDepositRefundDto>>> Get(string token, CancellationToken ct) =>
        Ok(ApiResponse<PublicDepositRefundDto>.Ok(await service.GetPublicAsync(token, ct)));

    [HttpPost("{token}/send-verification-code")]
    public async Task<ActionResult<ApiResponse<object>>> SendVerificationCode(string token, CancellationToken ct)
    {
        await service.SendVerificationCodeAsync(token, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Verification code sent."));
    }

    [HttpPost("{token}/verify")]
    public async Task<ActionResult<ApiResponse<PublicDepositRefundDto>>> Verify(string token, [FromBody] VerifyDepositRefundRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(ApiResponse<PublicDepositRefundDto>.Ok(await service.VerifyCustomerAsync(token, request, ct), "Customer verified."));
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(ApiResponse<PublicDepositRefundDto>.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : [ex.Message]));
        }
    }

    [HttpPost("{token}/submit-method")]
    public async Task<ActionResult<ApiResponse<PublicDepositRefundDto>>> SubmitMethod(string token, [FromBody] SubmitDepositRefundMethodRequest request, CancellationToken ct) =>
        Ok(ApiResponse<PublicDepositRefundDto>.Ok(await service.SubmitMethodAsync(token, request, ct), "Refund method submitted."));
}
