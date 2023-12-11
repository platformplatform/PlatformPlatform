using System.CommandLine;
using System.Reflection;
using PlatformPlatform.DeveloperCli.Installation;

ChangeDetection.EnsureCliIsCompiledWithLatestChanges(args);
AliasRegistration.EnsureAliasIsRegistered();
PrerequisitesChecker.EnsurePrerequisitesAreMet();

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