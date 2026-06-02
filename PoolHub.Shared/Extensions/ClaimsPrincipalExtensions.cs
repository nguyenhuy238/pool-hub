using System.Security.Claims;

namespace PoolHub.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return long.TryParse(raw, out var id) ? id : 0;
    }
}
