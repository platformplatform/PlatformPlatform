using System.CommandLine;
using System.Diagnostics;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class CheckCommand : Command
{
    public CheckCommand() : base("check", "Run build, test, format, and lint to verify everything before push")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Check backend code only" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Check frontend code only" };
        var cliOption = new Option<bool>("--cli", "-c") { Description = "Check developer-cli code only" };
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to check (e.g., main, account, back-office)" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(cliOption);
        Options.Add(selfContainedSystemOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(backendOption),
                parseResult.GetValue(frontendOption),
                parseResult.GetValue(cliOption),
                parseResult.GetValue(selfContainedSystemOption)
            )
        );
    }

    private static void Execute(bool backend, bool frontend, bool cli, string? selfContainedSystem)
    {
        var noFlags = !backend && !frontend && !cli;

        Prerequisite.Ensure(Prerequisite.Dotnet);
        Prerequisite.Ensure(Prerequisite.Node);

        var startTime = Stopwatch.GetTimestamp();

        var scsArgument = selfContainedSystem is not null ? $" --self-contained-system {selfContainedSystem}" : "";
        var backendFlag = backend || noFlags ? " --backend" : "";
        var frontendFlag = frontend || noFlags ? " --frontend" : "";
        var cliFlag = cli ? " --cli" : "";
        var targetFlags = $"{backendFlag}{frontendFlag}{cliFlag}";

        var runTests = backend || noFlags;
        var stepCount = runTests ? 4 : 3;
        var step = 1;

        AnsiConsole.MarkupLine($"[blue]Step {step++}/{stepCount}: Build[/]");
        ProcessHelper.Run($"dotnet run build{targetFlags}{scsArgument}", Configuration.CliFolder, "Build");

        if (runTests)
        {
            AnsiConsole.MarkupLine($"\n[blue]Step {step++}/{stepCount}: Test[/]");
            ProcessHelper.Run($"dotnet run test{scsArgument}", Configuration.CliFolder, "Test");
        }

        AnsiConsole.MarkupLine($"\n[blue]Step {step++}/{stepCount}: Format[/]");
        ProcessHelper.Run($"dotnet run format{targetFlags}{scsArgument}", Configuration.CliFolder, "Format");

        AnsiConsole.MarkupLine($"\n[blue]Step {step}/{stepCount}: Lint[/]");
        ProcessHelper.Run($"dotnet run lint{targetFlags}{scsArgument}", Configuration.CliFolder, "Lint");

        AnsiConsole.MarkupLine($"\n[green]All checks passed in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");
    }
}
