using System.CommandLine;
using System.Reflection;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

var isDebugBuild = new FileInfo(Environment.ProcessPath!).FullName.Contains("debug");
ChangeDetection.EnsureCliIsCompiledWithLatestChanges(isDebugBuild);

if (!Configuration.IsMacOs && !Configuration.IsWindows && !Configuration.IsLinux)
{
    AnsiConsole.MarkupLine($"[red]Your OS [bold]{Environment.OSVersion.Platform}[/] is not supported.[/]");
    Environment.Exit(1);
}

if (args.Length == 0)
{
    args = ["--help"];
}

// Preprocess arguments to handle @ symbols in search terms
args = CommandLineArgumentsPreprocessor.PreprocessArguments(args);

// Check if running MCP command - skip all output to keep stdout clean for MCP protocol
var isMcpCommand = args.Length > 0 && args[0] == "mcp";
var solutionName = new DirectoryInfo(Configuration.SourceCodeFolder).Name;

if (!isMcpCommand && !args.Contains("-q") && !args.Contains("--quiet"))
{
    if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "-?"))
    {
        var figletText = new FigletText(solutionName);
        AnsiConsole.Write(figletText);
    }

    AnsiConsole.WriteLine($"Source code folder: {Configuration.SourceCodeFolder} \n");
}

var rootCommand = new RootCommand
{
    Description = $"Welcome to the {solutionName} Developer CLI!"
};

var traceOption = new Option<bool>("--trace")
{
    Description = "Show external processes being executed",
    Recursive = true
};
rootCommand.Options.Add(traceOption);

var allCommands = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(Command)))
    .Select(Activator.CreateInstance)
    .Cast<Command>()
    .ToList();

if (!isDebugBuild)
{
    // Remove InstallCommand if isDebugBuild is false
    allCommands.Remove(allCommands.First(c => c.Name == "install"));
}

foreach (var command in allCommands)
{
    rootCommand.Subcommands.Add(command);
}

var parseResult = rootCommand.Parse(args);
Configuration.TraceEnabled = parseResult.GetValue(traceOption);
return await parseResult.InvokeAsync();
