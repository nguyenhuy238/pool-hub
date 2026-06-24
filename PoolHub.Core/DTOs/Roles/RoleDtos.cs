using System.ComponentModel.DataAnnotations;
using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Roles;

public class RoleQueryRequest : PaginationRequest
{
    public string? Keyword { get; set; }
}

public class RoleDto
{
    public long RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<string> PermissionCodes { get; set; } = [];
}

public class PermissionDto
{
    public long PermissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRolePermissionsRequest
{
    public List<long> PermissionIds { get; set; } = [];
}

public class CreateRoleRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; }
}

public class UpdateRoleRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
