namespace PoolHub.UnitTests;

public class TimePatternGuardTests
{
    [Fact]
    public void MainSource_DoesNotManuallyAddVietnamOffset()
    {
        var root = FindRepositoryRoot();
        var sourceFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}PoolHub.Services{Path.DirectorySeparatorChar}") ||
                           path.Contains($"{Path.DirectorySeparatorChar}PoolHub.API{Path.DirectorySeparatorChar}") ||
                           path.Contains($"{Path.DirectorySeparatorChar}PoolHub.Core{Path.DirectorySeparatorChar}") ||
                           path.Contains($"{Path.DirectorySeparatorChar}frontend{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}.next{Path.DirectorySeparatorChar}"));

        var offenders = sourceFiles
            .Where(path => File.ReadAllText(path).Contains("AddHours(7)", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(offenders);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "pool-hub.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}

