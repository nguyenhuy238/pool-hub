using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff + "," + RoleConstants.Cashier)]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerDto>>>> Get([FromQuery] CustomerQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<CustomerDto>>.Ok(await customerService.GetCustomersAsync(request, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetById(long id, CancellationToken ct) =>
        Ok(ApiResponse<CustomerDto>.Ok(await customerService.GetByIdAsync(id, ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await customerService.CreateAsync(request, User.GetUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.CustomerId }, ApiResponse<CustomerDto>.Ok(result, "Created"));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(long id, [FromBody] UpdateCustomerRequest request, CancellationToken ct) =>
        Ok(ApiResponse<CustomerDto>.Ok(await customerService.UpdateAsync(id, request, User.GetUserId(), ct), "Updated"));

    [HttpPatch("{id:long}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(long id, [FromBody] UpdateCustomerStatusRequest request, CancellationToken ct)
    {
        await customerService.UpdateStatusAsync(id, request.Status, User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Status updated"));
    }
}
