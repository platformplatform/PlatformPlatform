using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class GitHelper
{
    public static Commit[] ToCommits(this string consoleOutput)
    {
        return consoleOutput
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split(' ', 3))
            .Select(s => new Commit(s[0], s[1], s[2]))
            .ToArray();
    }
}

public record Commit
{
    public Commit(string date, string hash, string message)
    {
        ValidateHash(hash);
        Date = date;
        Hash = hash;
        Message = message;
    }

    public string Date { get; }

    public string Hash { get; }

    public string Message { get; }

    private static void ValidateHash(string hash)
    {
        if (!Regex.IsMatch(hash, "^[0-9a-fA-F]{6,}$"))
        {
            throw new ArgumentException("Invalid hash. Hash must be a hexadecimal string.", nameof(hash));
        }
    }

    public string GetFullCommitMessage()
    {
        return ProcessHelper.StartProcess($"git log -1 --pretty=%B {Hash}", Configuration.SourceCodeFolder, true);
    }
}
