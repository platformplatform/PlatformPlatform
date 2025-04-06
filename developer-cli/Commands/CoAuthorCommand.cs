using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public sealed class CoAuthorCommand : Command
{
    private const string CoAuthorTrailer = "Co-authored-by";

    public CoAuthorCommand() : base("coauthor", "Amends the current commit and adds you as a co-author")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private static void Execute()
    {
        var gitUserName = ProcessHelper.StartProcess("git config user.name", Configuration.SourceCodeFolder, true).Trim();
        var gitUserEmail = ProcessHelper.StartProcess("git config user.email", Configuration.SourceCodeFolder, true).Trim();

        if (string.IsNullOrEmpty(gitUserName) || string.IsNullOrEmpty(gitUserEmail))
        {
            AnsiConsole.MarkupLine("[red]Git user name or email not configured.[/]");
            Environment.Exit(1);
        }

        var commitAuthor = ProcessHelper.StartProcess(
            "git log -1 --format=\"%an <%ae>\"", Configuration.SourceCodeFolder, true, throwOnError: true
        ).Trim();
        var currentUser = $"{gitUserName} <{gitUserEmail}>";
        if (commitAuthor == currentUser)
        {
            AnsiConsole.MarkupLine("[yellow]You are already the author of this commit.[/]");
            Environment.Exit(0);
        }

        var stagedChanges = ProcessHelper.StartProcess("git diff --cached --name-only", Configuration.SourceCodeFolder, true);
        var hasNoChanges = string.IsNullOrWhiteSpace(stagedChanges);

        var commitMessage = ProcessHelper.StartProcess("git log -1 --format=%B", Configuration.SourceCodeFolder, true).Trim();
        var coAuthorLine = $"{CoAuthorTrailer}: {currentUser}";
        var isAlreadyCoAuthor = commitMessage.Contains(coAuthorLine);

        if (hasNoChanges && isAlreadyCoAuthor)
        {
            AnsiConsole.MarkupLine("[yellow]No staged changes, and you are already a co-author of this commit.[/]");
            Environment.Exit(0);
        }

        if (hasNoChanges && !AnsiConsole.Confirm("No staged changes. Do you want to add co-author information only?"))
        {
            Environment.Exit(0);
        }

        var amendMessage = isAlreadyCoAuthor ? "--no-edit" : $"-m \"{commitMessage.TrimEnd()}\n\n{coAuthorLine}\"";
        ProcessHelper.StartProcess($"git commit --amend {amendMessage}", Configuration.SourceCodeFolder);
        AnsiConsole.MarkupLine("[green]Successfully amended commit with co-author information.[/]");
    }
}
