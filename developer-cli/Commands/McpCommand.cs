using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class McpCommand : Command
{
    public McpCommand() : base("mcp", "Start MCP server for AI integration")
    {
        this.SetHandler(ExecuteAsync);
    }

    private static async Task ExecuteAsync()
    {
        // MCP server mode - all output to stderr to keep stdout clean
        Console.Error.WriteLine("[MCP] Starting MCP server...");
        Console.Error.WriteLine("[MCP] Listening on stdio for MCP communication");

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddConsole(consoleLogOptions =>
            {
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            }
        );
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        await builder.Build().RunAsync();
    }
}
[McpServerToolType]
public static class DeveloperCliMcpTools
{
    [McpServerTool]
    [Description("Build code")]
    public static string Build(
        [Description("Build backend/frontend/both")]
        bool backend = false,
        [Description("Build frontend")]
        bool frontend = false,
        [Description("Self-contained system (e.g., 'account-management')")]
        string? selfContainedSystem = null)
    {
        var args = new List<string> { "build" };
        var buildBackend = backend || !frontend;
        var buildFrontend = frontend || !backend;

        if (buildBackend && !buildFrontend) args.Add("--backend");
        if (buildFrontend && !buildBackend) args.Add("--frontend");
        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? $"Build completed.\n\n{result.Output}" : $"Build failed.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Run tests")]
    public static string Test(
        [Description("Self-contained system (e.g., 'account-management')")]
        string? selfContainedSystem = null,
        [Description("Skip build")]
        bool noBuild = false)
    {
        var args = new List<string> { "test" };
        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }
        if (noBuild) args.Add("--no-build");

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? $"Tests completed.\n\n{result.Output}" : $"Tests failed.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Format code")]
    public static string Format(
        [Description("Format backend")]
        bool backend = false,
        [Description("Format frontend")]
        bool frontend = false,
        [Description("Self-contained system (e.g., 'account-management')")]
        string? selfContainedSystem = null)
    {
        var args = new List<string> { "format" };
        var formatBackend = backend || !frontend;
        var formatFrontend = frontend || !backend;

        if (formatBackend && !formatFrontend) args.Add("--backend");
        if (formatFrontend && !formatBackend) args.Add("--frontend");
        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? $"Code formatted.\n\n{result.Output}" : $"Formatting failed.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Run code inspections")]
    public static string Inspect(
        [Description("Inspect backend")]
        bool backend = false,
        [Description("Inspect frontend")]
        bool frontend = false,
        [Description("Self-contained system (e.g., 'account-management')")]
        string? selfContainedSystem = null,
        [Description("Skip build")]
        bool noBuild = false)
    {
        var args = new List<string> { "inspect" };
        var inspectBackend = backend || !frontend;
        var inspectFrontend = frontend || !backend;

        if (inspectBackend && !inspectFrontend) args.Add("--backend");
        if (inspectFrontend && !inspectBackend) args.Add("--frontend");
        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }
        if (noBuild) args.Add("--no-build");

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? $"Inspections completed.\n\n{result.Output}" : $"Inspections found issues.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Run all checks")]
    public static string Check(
        [Description("Check backend")]
        bool backend = false,
        [Description("Check frontend")]
        bool frontend = false,
        [Description("Self-contained system (e.g., 'account-management')")]
        string? selfContainedSystem = null)
    {
        var args = new List<string> { "check" };
        var checkBackend = backend || !frontend;
        var checkFrontend = frontend || !backend;

        if (checkBackend && !checkFrontend) args.Add("--backend");
        if (checkFrontend && !checkBackend) args.Add("--frontend");
        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? $"All checks passed.\n\n{result.Output}" : $"Checks failed.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Start .NET Aspire at https://localhost:9000")]
    public static string Watch()
    {
        var args = new List<string> { "watch", "--detach", "--force" };
        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? $"Aspire started.\n\n{result.Output}" : $"Failed to start.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Run E2E tests")]
    public static string E2e(
        [Description("Search terms")]
        string[] searchTerms = null!,
        [Description("Browser")]
        string browser = "all",
        [Description("Smoke only")]
        bool smoke = false)
    {
        var args = new List<string> { "e2e", "--quiet" };
        if (searchTerms is { Length: > 0 }) args.AddRange(searchTerms);
        if (browser != "all")
        {
            args.Add("--browser");
            args.Add(browser);
        }
        if (smoke) args.Add("--smoke");

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? $"E2E tests completed.\n\n{result.Output}" : $"E2E tests failed.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Initialize task-manager directory in .workspace as separate git repository")]
    public static string InitTaskManager()
    {
        var result = ExecuteCliCommand(["init-task-manager"]);
        return result.Success ? "Task-manager initialized successfully" : $"Failed to initialize task-manager.\n\n{result.Output}";
    }

    private static (bool Success, string Output) ExecuteCliCommand(string[] args)
    {
        var outputLines = new List<string>();
        var errorLines = new List<string>();

        var developerCliPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli");
        var allArgs = new List<string> { "run", "--project", developerCliPath };
        allArgs.AddRange(args);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", allArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
            WorkingDirectory = Configuration.SourceCodeFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) outputLines.Add(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) errorLines.Add(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        var allOutput = string.Join("\n", outputLines);
        if (errorLines.Count > 0) allOutput += "\n\nErrors:\n" + string.Join("\n", errorLines);

        return (process.ExitCode == 0, allOutput);
    }
}
