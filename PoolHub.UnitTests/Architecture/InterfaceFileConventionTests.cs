using System.Text.RegularExpressions;

namespace PoolHub.UnitTests;

public class InterfaceFileConventionTests
{
    [Fact]
    public void CoreAndInfrastructurePublicInterfaces_AreDeclaredInMatchingFiles()
    {
        var root = ArchitectureTestHelpers.FindRepositoryRoot();
        var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => IsInterfaceContractPath(root, path))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        var violations = new List<string>();
        foreach (var file in files)
        {
            var interfaceNames = Regex.Matches(File.ReadAllText(file), @"public\s+interface\s+([A-Za-z_][A-Za-z0-9_]*)")
                .Select(match => match.Groups[1].Value)
                .ToList();

            if (interfaceNames.Count == 0)
            {
                continue;
            }

            var fileName = Path.GetFileNameWithoutExtension(file);
            if (interfaceNames.Count != 1 || !string.Equals(fileName, interfaceNames[0], StringComparison.Ordinal))
            {
                violations.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.Empty(violations);
    }

    private static bool IsInterfaceContractPath(string root, string path)
    {
        var relativePath = Path.GetRelativePath(root, path);
        return relativePath.StartsWith($"PoolHub.Core{Path.DirectorySeparatorChar}Interfaces{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
               relativePath.StartsWith($"PoolHub.Infrastructure{Path.DirectorySeparatorChar}Repositories{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
