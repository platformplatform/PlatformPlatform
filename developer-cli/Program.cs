using System.CommandLine;
using System.Reflection;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

var isDebugBuild = new FileInfo(Environment.ProcessPath!).FullName.Contains("debug");
ChangeDetection.EnsureCliIsCompiledWithLatestChanges(isDebugBuild);

if (!Configuration.IsMacOs && !Configuration.IsWindows)
{
    AnsiConsole.MarkupLine($"[red]Your OS [bold]{Environment.OSVersion.Platform}[/] is not supported.[/]");
    Environment.Exit(1);
}

if (args.Length == 0)
{
    args = ["--help"];
}

if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "-?"))
{
    var figletText = new FigletText("PlatformPlatform");
    AnsiConsole.Write(figletText);
}

var rootCommand = new RootCommand
{
    Description = "Welcome to the PlatformPlatform Developer CLI!"
};

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

allCommands.ForEach(rootCommand.AddCommand);

await rootCommand.InvokeAsync(args);