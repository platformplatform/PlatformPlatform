using System.CommandLine;
using System.Reflection;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

if (!Environment.IsMacOs && !Environment.IsWindows)
{
    AnsiConsole.MarkupLine($"[red]Your OS [bold]{System.Environment.OSVersion.Platform}[/] is not supported.[/]");
    System.Environment.Exit(1);
}

ChangeDetection.EnsureCliIsCompiledWithLatestChanges(args);
AliasRegistration.EnsureAliasIsRegistered();
PrerequisitesChecker.EnsurePrerequisitesAreMeet(args);

var rootCommand = new RootCommand
{
    Description = "Welcome to the PlatformPlatform Developer CLI!"
};

var allCommands = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(Command)))
    .Select(Activator.CreateInstance)
    .Cast<Command>()
    .ToList();

allCommands.ForEach(rootCommand.AddCommand);

await rootCommand.InvokeAsync(args);