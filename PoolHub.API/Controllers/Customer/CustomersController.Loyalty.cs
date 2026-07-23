using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

public partial class CustomersController
{
    [HttpPost("{id:long}/exchange-voucher/{templateId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> ExchangeVoucher(long id, long templateId, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _customerService.ExchangeVoucherAsync(id, templateId, User.GetUserId(), ct), "Đổi voucher thành công."));

    [HttpGet("{id:long}/point-history")]
    public async Task<ActionResult<ApiResponse<object>>> PointHistory(long id, [FromQuery] PaginationRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _customerService.GetPointHistoryAsync(id, request, ct)));
}
