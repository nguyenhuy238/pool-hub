using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Users;

public class UpdateUserRoleRequest
{
    [Required]
    public List<string> Roles { get; set; } = [];
}
