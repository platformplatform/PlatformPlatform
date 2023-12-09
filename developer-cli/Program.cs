using PlatformPlatform.DeveloperCli;
using Spectre.Console;

ChangeDetection.EnsureCliIsCompiledWithLatestChanges(args);

var command = args.FirstOrDefault();
AnsiConsole.MarkupLine($"Unknown command [green]{command}[/]. Use [green]--help[/] to see available commands.");