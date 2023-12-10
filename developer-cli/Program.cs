using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

ChangeDetection.EnsureCliIsCompiledWithLatestChanges(args);
AliasRegistration.EnsureAliasIsRegistered();
PrerequisitesChecker.EnsurePrerequisitesAreMet();

var command = args.FirstOrDefault();

switch (command)
{
    case null:
    case "--help":
    case "-h":
        AnsiConsole.MarkupLine(
            """
            [green]Welcome to the PlatformPlatform CLI:[/]

            Here are the base commands:
            
               [yellow]No commands implemented yet![/]

            Use `--version` to display the current version.
            """);
        break;

    default:
        AnsiConsole.MarkupLine(
            $"""
             [red]'{command}' is misspelled or an unknown command[/].

             Please use [green]-help[/] to see the list of available commands.
             """);
        Environment.ExitCode = 1;
        break;
}