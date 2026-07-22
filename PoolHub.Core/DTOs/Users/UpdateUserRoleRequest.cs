using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Users;

public class UpdateUserRoleRequest
{
    [Required]
    public List<long> RoleIds { get; set; } = [];

    public List<string> Roles { get; set; } = [];
}
