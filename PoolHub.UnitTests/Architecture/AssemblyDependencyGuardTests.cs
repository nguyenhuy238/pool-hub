using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Auth;
using PoolHub.Shared;
using System.Reflection;

namespace PoolHub.UnitTests;

public class AssemblyDependencyGuardTests
{
    public static IEnumerable<object[]> ForbiddenAssemblyReferences =>
    [
        [typeof(ApiResponse<>).Assembly, "PoolHub.Shared", new[] { "PoolHub.API", "PoolHub.Core", "PoolHub.Infrastructure", "PoolHub.Services" }],
        [typeof(User).Assembly, "PoolHub.Core", new[] { "PoolHub.API", "PoolHub.Infrastructure", "PoolHub.Services" }],
        [typeof(PoolHubDbContext).Assembly, "PoolHub.Infrastructure", new[] { "PoolHub.API", "PoolHub.Services" }],
        [typeof(AuthService).Assembly, "PoolHub.Services", new[] { "PoolHub.API" }]
    ];

    [Theory]
    [MemberData(nameof(ForbiddenAssemblyReferences))]
    public void ProjectAssembly_DoesNotReferenceForbiddenPoolHubAssemblies(
        Assembly assembly,
        string assemblyName,
        string[] forbiddenReferences)
    {
        var actualReferences = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        var violations = forbiddenReferences
            .Where(actualReferences.Contains)
            .ToList();

        Assert.Empty(violations);
        Assert.Equal(assemblyName, assembly.GetName().Name);
    }
}
