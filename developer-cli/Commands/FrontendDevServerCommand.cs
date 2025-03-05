using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class FrontendDevServerCommand : Command
{
    public FrontendDevServerCommand() : base("frontend-dev-server", "Run the frontend development server")
    {
        AddOption(new Option<string?>(
                ["<solution-name>", "--solution-name", "-s"],
                "The name of the self-contained system to run"
            )
        );
        AddOption(new Option<bool>(["--create-flag-file"], "Create a flag file to prevent another frontend server from starting"));

        Handler = CommandHandler.Create<string?, bool>(Execute);
    }

    private int Execute(string? solutionName, bool createFlagFile)
    {
        Prerequisite.Ensure(Prerequisite.Node);

        if (File.Exists(Configuration.FrontendDevServerFlagFile))
        {
            AnsiConsole.WriteLine($"Frontend development server is already running. If you believe this to be mistake, delete the flag file {Configuration.FrontendDevServerFlagFile}");
            return 0;
        }

        var solutionFile = SolutionHelper.GetSolution(solutionName);

        var environmentVariables = new Dictionary<string, string>();

        // CERTIFICATE_PASSWORD is set by AppHost, but when running standalone we need to set it up
        if (Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD") == null)
        {
            environmentVariables.Add("CERTIFICATE_PASSWORD", GetCertificatePasswordFromAppHost(solutionFile));
        }

        if (createFlagFile)
        {
            File.WriteAllBytes(Configuration.FrontendDevServerFlagFile, Array.Empty<byte>());

            AppDomain.CurrentDomain.ProcessExit += (_, _) => File.Delete(Configuration.FrontendDevServerFlagFile);

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // Prevent immediate termination, so the flag file gets deleted
                File.Delete(Configuration.FrontendDevServerFlagFile);
            };
        }

        ProcessHelper.StartProcess("npm run dev", solutionFile.Directory?.FullName, throwOnError: false, exitOnError: false, environmentVariables: environmentVariables);
        return 0;
    }

    private static string GetCertificatePasswordFromAppHost(FileInfo solutionFile)
    {
        var secretsJson = ProcessHelper.StartProcess("dotnet user-secrets list -p AppHost --json", solutionFile.Directory?.FullName, true);

        var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(secretsJson, new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip });
        if (secrets?.ContainsKey("certificate-password") != true)
        {
            AnsiConsole.WriteLine("[red]Could not get certificate password from AppHost user secrets. Start by running the frontend development server as part of AppHost, and then try again.[/]");
            Environment.Exit(1);
        }

        var secret = secrets["certificate-password"];
        return secret;
    }
}
