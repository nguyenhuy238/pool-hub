using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

/// <summary>
/// Quan ly thong tin khach hang.
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager + "," + RoleConstants.Staff, Policy = PermissionConstants.CustomersManage)]
public partial class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }
}
