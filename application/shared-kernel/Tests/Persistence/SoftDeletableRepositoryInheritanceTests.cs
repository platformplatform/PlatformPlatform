using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.Persistence;

public sealed class SoftDeletableRepositoryInheritanceTests
{
    [Fact]
    public void SoftDeletableRepository_WhenImplemented_ShouldExtendSoftDeletableRepositoryBase()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        var csFiles = Directory.GetFiles(repositoryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}")
            );

        // Act
        var violations = new List<string>();
        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("ISoftDeletableRepository")) continue;

            // File declares an interface extending ISoftDeletableRepository, so the implementation must use SoftDeletableRepositoryBase
            if (content.Contains("RepositoryBase<") && !content.Contains("SoftDeletableRepositoryBase<"))
            {
                violations.Add(Path.GetRelativePath(repositoryRoot, file));
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "Repositories for soft-deletable aggregates must extend SoftDeletableRepositoryBase, not RepositoryBase. "
            + "SoftDeletableRepositoryBase overrides GetByIdAsync to respect the soft-delete query filter. "
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
