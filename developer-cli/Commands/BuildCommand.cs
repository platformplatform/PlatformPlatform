using System.CommandLine;
using System.Diagnostics;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Builds a self-contained system")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Build backend code" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Build frontend code" };
        var emailsOption = new Option<bool>("--emails", "-e") { Description = "Build email templates" };
        var cliOption = new Option<bool>("--cli", "-c") { Description = "Build developer-cli code" };
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to build (e.g., main, account, back-office)" };
        var gatewayOption = new Option<bool>("--gateway", "-g") { Description = "Scope backend work to AppGateway and AppGateway.Tests" };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Minimal output mode" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(emailsOption);
        Options.Add(cliOption);
        Options.Add(selfContainedSystemOption);
        Options.Add(gatewayOption);
        Options.Add(quietOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(backendOption),
                parseResult.GetValue(frontendOption),
                parseResult.GetValue(emailsOption),
                parseResult.GetValue(cliOption),
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(gatewayOption),
                parseResult.GetValue(quietOption)
            )
        );
    }

    private static void Execute(bool backend, bool frontend, bool emails, bool developerCli, string? selfContainedSystem, bool gateway, bool quiet)
    {
        if (gateway) AppGatewayHelper.EnsureNotCombinedWithSelfContainedSystem(selfContainedSystem);

        var noFlags = !backend && !frontend && !emails && !developerCli;
        var buildBackend = backend || noFlags;
        var buildFrontend = frontend || noFlags;
        var buildDeveloperCli = developerCli || noFlags;
        // --frontend and the no-flag default both build emails as part of the turbo run, so the
        // standalone email build only fires when the user passes --emails without --frontend.
        var buildEmailsStandalone = emails && !buildFrontend;

        // Ensure prerequisites based on what we're building
        if (buildBackend || buildDeveloperCli) Prerequisite.Ensure(Prerequisite.Dotnet);
        if (buildFrontend || buildEmailsStandalone) Prerequisite.Ensure(Prerequisite.Node);

        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;
            var emailsTime = TimeSpan.Zero;
            var developerCliTime = TimeSpan.Zero;

            if (buildBackend)
            {
                if (!quiet) AnsiConsole.MarkupLine("[blue]Running backend build...[/]");

                if (gateway)
                {
                    ProcessHelper.Run($"dotnet build {AppGatewayHelper.TestProjectRelativePath}", Configuration.ApplicationFolder, "Build", quiet);
                }
                else
                {
                    var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);
                    ProcessHelper.Run($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName, "Build", quiet);
                }

                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (buildFrontend)
            {
                if (!quiet) AnsiConsole.MarkupLine("[blue]Ensure npm packages are up to date...[/]");
                ProcessHelper.Run("npm install --silent", Configuration.ApplicationFolder, "npm install", quiet);

                if (!quiet) AnsiConsole.MarkupLine("\n[blue]Running frontend build...[/]");
                RunFrontendBuild(quiet);
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            if (buildEmailsStandalone)
            {
                if (!quiet) AnsiConsole.MarkupLine("[blue]Ensure npm packages are up to date...[/]");
                ProcessHelper.Run("npm install --silent", Configuration.ApplicationFolder, "npm install", quiet);

                if (!quiet) AnsiConsole.MarkupLine("\n[blue]Running emails build...[/]");
                RunEmailsBuild(quiet);
                emailsTime = Stopwatch.GetElapsedTime(startTime) - backendTime - frontendTime;
            }

            if (buildDeveloperCli)
            {
                if (!quiet) AnsiConsole.MarkupLine("[blue]Running developer-cli build...[/]");
                RunDeveloperCliBuild(quiet);
                developerCliTime = Stopwatch.GetElapsedTime(startTime) - backendTime - frontendTime - emailsTime;
            }

            if (quiet)
            {
                Console.WriteLine("Build succeeded.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Build completed successfully in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");

                var multipleTargets = (buildBackend ? 1 : 0) + (buildFrontend ? 1 : 0) + (buildEmailsStandalone ? 1 : 0) + (buildDeveloperCli ? 1 : 0) > 1;
                if (multipleTargets)
                {
                    var timingLines = new List<string>();
                    if (buildBackend) timingLines.Add($"Backend:       [green]{backendTime.Format()}[/]");
                    if (buildFrontend) timingLines.Add($"Frontend:      [green]{frontendTime.Format()}[/]");
                    if (buildEmailsStandalone) timingLines.Add($"Emails:        [green]{emailsTime.Format()}[/]");
                    if (buildDeveloperCli) timingLines.Add($"Developer CLI: [green]{developerCliTime.Format()}[/]");
                    AnsiConsole.MarkupLine(string.Join(Environment.NewLine, timingLines));
                }
            }
        }
        catch (Exception ex)
        {
            if (quiet)
            {
                Console.WriteLine($"Build failed: {ex.Message}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error during build: {ex.Message}[/]");
            }

            Environment.Exit(1);
        }
    }

    private static void RunFrontendBuild(bool quiet)
    {
        if (quiet)
        {
            var result = ProcessHelper.ExecuteQuietly("npm run build", Configuration.ApplicationFolder);
            if (!result.Success)
            {
                var errors = ExtractFrontendErrors(result.CombinedOutput);
                Console.WriteLine("Frontend build failed.");
                Console.WriteLine();
                Console.WriteLine($"Errors ({errors.Count}):");
                foreach (var error in errors.Take(3))
                {
                    Console.WriteLine($"  {error}");
                }

                if (errors.Count > 3)
                {
                    Console.WriteLine($"  ... and {errors.Count - 3} more error(s)");
                }

                Console.WriteLine();
                Console.WriteLine($"Full output: {result.TempFilePathWithSize}");
                Environment.Exit(1);
            }
        }
        else
        {
            ProcessHelper.StartProcess("npm run build", Configuration.ApplicationFolder);
        }
    }

    private static List<string> ExtractFrontendErrors(string output)
    {
        var errors = new List<string>();
        var lines = output.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Look for TypeScript error patterns like "Error: TS2551:" or "× Error:"
            if (line.Contains("Error: TS") || line.Contains("× Error:"))
            {
                // Extract file and error message
                var errorMessage = line;

                // Try to get the file path from previous lines
                for (var j = i - 1; j >= Math.Max(0, i - 3); j--)
                {
                    if (lines[j].Contains("File:") || lines[j].Contains(".tsx:") || lines[j].Contains(".ts:"))
                    {
                        var fileLine = lines[j].Trim();
                        errorMessage = $"{fileLine} - {errorMessage}";
                        break;
                    }
                }

                errors.Add(errorMessage.Replace("[0m", "").Replace("[31m", "").Replace("[39m", ""));
            }
        }

        // If no TypeScript errors found, look for generic error messages
        if (errors.Count == 0)
        {
            foreach (var line in lines)
            {
                if (line.Contains("error") && !line.Contains("npm error") && line.Length < 200)
                {
                    errors.Add(line.Trim());
                }
            }
        }

        return errors.Count > 0 ? errors : ["Build failed. See full output for details."];
    }

    private static void RunDeveloperCliBuild(bool quiet)
    {
        var solutionFile = new FileInfo(Path.Combine(Configuration.CliFolder, "DeveloperCli.slnx"));
        ProcessHelper.Run($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName, "Build", quiet);
    }

    private static void RunEmailsBuild(bool quiet)
    {
        // The full frontend build (turbo build) already runs the @repo/emails package's build script;
        // when --emails is the only target we filter turbo down to that workspace so authors can iterate
        // on templates without paying for the whole SPA build.
        var command = "npx turbo build --filter=@repo/emails";
        if (quiet)
        {
            var result = ProcessHelper.ExecuteQuietly(command, Configuration.ApplicationFolder);
            if (!result.Success)
            {
                Console.WriteLine("Emails build failed.");
                Console.WriteLine();
                Console.WriteLine($"Full output: {result.TempFilePathWithSize}");
                Environment.Exit(1);
            }
        }
        else
        {
            ProcessHelper.StartProcess(command, Configuration.ApplicationFolder);
        }
    }
}
