namespace MiniHn.Api.Tests;

public sealed class DependencyGraphTests
{
    [Fact]
    public void No_sqlite_package_remains_in_the_dependency_graph()
    {
        var root = RepositoryRoot();
        var separator = Path.DirectorySeparatorChar;
        var lockFiles = Directory.GetFiles(root, "packages.lock.json", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{separator}obj{separator}", StringComparison.Ordinal)
                           && !path.Contains($"{separator}bin{separator}", StringComparison.Ordinal))
            .ToList();

        lockFiles.Should().NotBeEmpty("every project restores against a committed lock file");

        foreach (var lockFile in lockFiles)
        {
            File.ReadAllText(lockFile).Should().NotContainEquivalentOf("sqlite",
                $"PostgreSQL is the only provider, so {Path.GetRelativePath(root, lockFile)} must not pull SQLite in");
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MiniHn.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("MiniHn.sln not found above the test output");
    }
}
