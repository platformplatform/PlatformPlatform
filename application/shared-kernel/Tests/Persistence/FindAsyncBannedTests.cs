using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.Persistence;

public sealed class FindAsyncBannedTests
{
    [Fact]
    public void FindAsync_WhenUsedInProductionCode_ShouldBeRejected()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "RepositoryBase.cs" };

        var csFiles = Directory.GetFiles(repositoryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}")
            );

        // Act
        var violations = new List<string>();
        foreach (var file in csFiles)
        {
            if (allowedFiles.Contains(Path.GetFileName(file))) continue;

            var content = File.ReadAllText(file);
            if (content.Contains(".FindAsync("))
            {
                violations.Add(Path.GetRelativePath(repositoryRoot, file));
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "FindAsync bypasses EF Core query filters (including soft-delete). "
            + "Use the repository's GetByIdAsync method instead. "
            + $"Violations found in: {string.Join(", ", violations)}"
        );
    }

    private static string GetRepositoryRoot([CallerFilePath] string callerFilePath = "")
    {
        var directory = Path.GetDirectoryName(callerFilePath)!;
        while (!string.IsNullOrEmpty(directory) && !Directory.Exists(Path.Combine(directory, ".git")))
        {
            directory = Path.GetDirectoryName(directory)!;
        }

        return directory;
    }
}
