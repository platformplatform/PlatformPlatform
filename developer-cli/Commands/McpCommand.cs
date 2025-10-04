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
    [Description("Build code. Builds both backend and frontend by default. Use backend/frontend flags to build only one.")]
    public static string Build(
        [Description("Build backend only (optional)")]
        bool backend = false,
        [Description("Build frontend only (optional)")]
        bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
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
    [Description("Run tests. Tests entire codebase by default.")]
    public static string Test(
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
        string? selfContainedSystem = null,
        [Description("Skip build (optional)")]
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
        return result.Success
            ? SummarizeTestSuccess(result.Output)
            : $"Tests failed.\n\n{TruncateOutput(result.Output, 2000)}";
    }

    [McpServerTool]
    [Description("Format code. Formats both backend and frontend by default. Use backend/frontend flags to format only one.")]
    public static string Format(
        [Description("Format backend only (optional)")]
        bool backend = false,
        [Description("Format frontend only (optional)")]
        bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
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
    [Description("Run code inspections. Inspects both backend and frontend by default. Use backend/frontend flags to inspect only one.")]
    public static string Inspect(
        [Description("Inspect backend only (optional)")]
        bool backend = false,
        [Description("Inspect frontend only (optional)")]
        bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
        string? selfContainedSystem = null,
        [Description("Skip build (optional)")]
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
        return result.Success
            ? "Inspections completed successfully. Results saved to /application/result.json"
            : $"Inspections found issues.\n\n{TruncateOutput(result.Output, 2000)}";
    }

    [McpServerTool]
    [Description("Run all checks (build + test + format + inspect). Checks both backend and frontend by default. Use backend/frontend flags to check only one.")]
    public static string Check(
        [Description("Check backend only (optional)")]
        bool backend = false,
        [Description("Check frontend only (optional)")]
        bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
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
        return result.Success
            ? "All checks passed (build + test + format + inspect)."
            : $"Checks failed.\n\n{TruncateOutput(result.Output, 3000)}";
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

    [McpServerTool]
    [Description("Sync AI rules from .claude to .windsurf and .cursor. CRITICAL: Always make AI rule changes in .claude folder first, then sync")]
    public static string SyncAiRules()
    {
        var result = ExecuteCliCommand(["sync-ai-rules"]);
        return result.Success ? "AI rules synced successfully to .windsurf and .cursor" : $"Failed to sync AI rules.\n\n{result.Output}";
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

    private static string SummarizeTestSuccess(string output)
    {
        // Extract test summary line (e.g., "Passed! - Failed: 0, Passed: 42, Skipped: 0, Total: 42")
        var lines = output.Split('\n');
        var summaryLine = lines.FirstOrDefault(l => l.Contains("Passed!") || l.Contains("Failed:"));

        if (summaryLine != null)
        {
            return $"Tests passed.\n\n{summaryLine}";
        }

        // Fallback: just confirm success without massive output
        return "Tests passed successfully.";
    }

    private static string TruncateOutput(string output, int maxChars)
    {
        if (output.Length <= maxChars)
        {
            return output;
        }

        var halfSize = maxChars / 2;
        var beginning = output[..halfSize];
        var ending = output[^halfSize..];

        var lines = output.Split('\n');
        var totalLines = lines.Length;

        return $"{beginning}\n\n... [Output truncated: {output.Length - maxChars} chars omitted, {totalLines} total lines] ...\n\n{ending}";
    }
}
