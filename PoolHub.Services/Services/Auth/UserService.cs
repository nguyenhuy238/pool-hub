using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services.Auth;

public class UserService(PoolHubDbContext db) : IUserService
{
    public async Task<PagedResult<UserDto>> GetUsersAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.FullName.Contains(request.Search) || x.Email.Contains(request.Search));
        var total = await query.CountAsync(cancellationToken);
        var users = await query.OrderBy(x => x.UserId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.RoleId select new { ur.UserId, r.Name }).ToListAsync(cancellationToken);
        return new PagedResult<UserDto>
        {
            Items = users.Select(u => new UserDto { UserId = u.UserId, PublicId = u.PublicId, FullName = u.FullName, Email = u.Email, PhoneNumber = u.PhoneNumber, Status = u.Status, Roles = roles.Where(x => x.UserId == u.UserId).Select(x => x.Name).ToList() }).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken) ?? throw new NotFoundException("User not found.");
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.RoleId where ur.UserId == id select r.Name).ToListAsync(cancellationToken);
        return new UserDto { UserId = user.UserId, PublicId = user.PublicId, FullName = user.FullName, Email = user.Email, PhoneNumber = user.PhoneNumber, Status = user.Status, Roles = roles };
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(x => x.Email == request.Email.ToLower(), cancellationToken)) throw new ConflictException("Email already exists.");
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Name == request.Role, cancellationToken) ?? throw new ValidationException("Role not found.");
        var user = new User { FullName = request.FullName, Email = request.Email.ToLower(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12), Status = true, EmailConfirmed = true };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(user.UserId, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken) ?? throw new NotFoundException("User not found.");
        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl;
        user.Status = request.Status;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task UpdateRolesAsync(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken) ?? throw new NotFoundException("User not found.");
        var roleIds = await db.Roles.Where(x => request.Roles.Contains(x.Name)).Select(x => x.RoleId).ToListAsync(cancellationToken);
        if (roleIds.Count != request.Roles.Count) throw new ValidationException("One or more roles are invalid.");

        var olds = db.UserRoles.Where(x => x.UserId == id);
        db.UserRoles.RemoveRange(olds);
        foreach (var roleId in roleIds) db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = roleId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(int id, bool status, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([id], cancellationToken) ?? throw new NotFoundException("User not found.");
        user.Status = status;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<string>> GetRolesAsync(CancellationToken cancellationToken) => db.Roles.OrderBy(x => x.RoleId).Select(x => x.Name).ToListAsync(cancellationToken);
}
