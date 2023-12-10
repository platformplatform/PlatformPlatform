using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

ChangeDetection.EnsureCliIsCompiledWithLatestChanges(args);
AliasRegistration.EnsureAliasIsRegistered();

var command = args.FirstOrDefault();
AnsiConsole.MarkupLine($"Unknown command [green]{command}[/]. Use [green]--help[/] to see available commands.");