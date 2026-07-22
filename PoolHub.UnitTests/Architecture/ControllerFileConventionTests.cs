using System.Text.RegularExpressions;

namespace PoolHub.UnitTests;

public class ControllerFileConventionTests
{
    [Fact]
    public void ApiControllerFiles_DeclareSingleMatchingControllerClass()
    {
        var root = ArchitectureTestHelpers.FindRepositoryRoot();
        var controllersRoot = Path.Combine(root, "PoolHub.API", "Controllers");
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(controllersRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var controllerNames = Regex.Matches(text, @"public\s+class\s+([A-Za-z_][A-Za-z0-9_]*Controller)\b")
                .Select(match => match.Groups[1].Value)
                .ToList();

            if (controllerNames.Count == 0)
            {
                continue;
            }

            var fileName = Path.GetFileNameWithoutExtension(file);
            if (controllerNames.Count != 1 || !string.Equals(fileName, controllerNames[0], StringComparison.Ordinal))
            {
                violations.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.Empty(violations);
    }
}
