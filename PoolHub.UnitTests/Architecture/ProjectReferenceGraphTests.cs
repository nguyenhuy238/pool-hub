using System.Xml.Linq;

namespace PoolHub.UnitTests;

public class ProjectReferenceGraphTests
{
    private static readonly Dictionary<string, string[]> ExpectedProjectReferences = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PoolHub.API"] = ["PoolHub.Services", "PoolHub.Infrastructure", "PoolHub.Shared", "PoolHub.Core"],
        ["PoolHub.Core"] = ["PoolHub.Shared"],
        ["PoolHub.Infrastructure"] = ["PoolHub.Core", "PoolHub.Shared"],
        ["PoolHub.Services"] = ["PoolHub.Core", "PoolHub.Infrastructure", "PoolHub.Shared"],
        ["PoolHub.Shared"] = [],
        ["PoolHub.UnitTests"] = ["PoolHub.Services", "PoolHub.Infrastructure", "PoolHub.Core"],
        ["PoolHub.IntegrationTests"] = ["PoolHub.API"],
        ["pool-hub"] = []
    };

    [Fact]
    public void ProjectReferences_MatchCurrentDependencyGraph()
    {
        var root = ArchitectureTestHelpers.FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var (projectName, expectedReferences) in ExpectedProjectReferences)
        {
            var projectPath = FindProjectPath(root, projectName);
            var actualReferences = XDocument.Load(projectPath)
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFileNameWithoutExtension(value!))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var expected = expectedReferences
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (!actualReferences.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add($"{projectName}: expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actualReferences)}]");
            }
        }

        Assert.Empty(violations);
    }

    private static string FindProjectPath(string root, string projectName)
    {
        var projectPath = Directory.EnumerateFiles(root, $"{projectName}.csproj", SearchOption.AllDirectories)
            .FirstOrDefault(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                    !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

        return projectPath ?? throw new InvalidOperationException($"Project file not found for {projectName}.");
    }
}
