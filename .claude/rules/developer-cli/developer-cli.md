---
paths: developer-cli/**/*.cs
description: Rules for implementing Developer CLI commands
---

# Developer Command Line Interface Rules

Guidelines for implementing and extending the custom Developer Command Line Interface (CLI) commands.

## Implementation

1. Command Structure:
   - Create one file per command in `developer-cli/Commands`
   - Name the file with `Command` suffix and inherit from `System.CommandLine.Command`
   - Provide a concise description in the constructor explaining the command's purpose
   - Define command options and register them using `Options.Add()`
   - Implement the command's logic in a private `Execute` method
   - Use static methods where appropriate for better organization

2. Command Options:
   - Use double-dash (`--`) for long names and single-dash (`-`) for abbreviations
   - Provide clear, concise descriptions for all options
   - Use consistent naming across commands (e.g., `--self-contained-system` and `-s`)
   - Define option types explicitly (e.g., `Option<bool>`, `Option<string?>`)
   - For positional arguments, include both positional and named options
   - Set default values where appropriate using lambda expressions

3. Prerequisites and Dependencies:
   - Always check for required dependencies at the beginning of `Execute`
   - Use `Prerequisite.Ensure()` to verify required tools are installed
   - Common prerequisites: `Prerequisite.Dotnet` and `Prerequisite.Node`

4. Process Execution:
   - Use `ProcessHelper` for all external process execution
   - Use `ProcessHelper.Run()` for standard operations (handles quiet vs verbose output automatically)
   - Use `ProcessHelper.ExecuteQuietly()` to capture output without showing user feedback
   - Use `ProcessHelper.StartProcess()` for direct execution with special cases
   - Specify working directory as the second parameter when needed

5. Error Handling:
   - Use `try/catch` blocks to handle exceptions
   - Display error messages using `AnsiConsole.MarkupLine()` with color formatting
   - Use `Environment.Exit(1)` to exit with non-zero status on errors
   - Don't throw exceptions—handle them and exit gracefully
   - Provide clear, actionable error messages

6. Console Output:
   - Use `Spectre.Console.AnsiConsole` for all console output
   - Use color coding: `[blue]` info, `[green]` success, `[yellow]` warnings, `[red]` errors
   - Format output consistently across all commands
   - Use tables, panels, or other Spectre.Console features for complex output

7. Command Registration:
   - Set the command handler using `SetAction(parseResult => Execute(...))` in the constructor
   - Use `parseResult.GetValue(option)` to map options to handler parameters
   - Use nullable types for optional parameters

8. Utility Classes:
   - Use existing utility classes from `developer-cli/Utilities`
   - Only create new utility classes for truly generic functionality
   - Place command-specific helper methods as private methods in the command class

9. Performance Tracking:
   - Use `Stopwatch` to track execution time for long-running operations
   - Display timing information for better feedback
   - Format timing consistently using extension methods like `.Format()`

10. Self-Contained Implementation:
    - Keep each command self-contained in a single file
    - Avoid dependencies between command implementations
    - Extract shared functionality to utility classes only when necessary

## Examples

```csharp
// ✅ DO: Use clear option naming, prerequisite checks, AnsiConsole, ProcessHelper
public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Builds the solution")
    {
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The self-contained system to build" }; // ✅ DO: Constructor-style option definition
        var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Enable verbose output" };

        Options.Add(selfContainedSystemOption); // ✅ DO: Register with Options.Add()
        Options.Add(verboseOption);

        SetAction(parseResult => Execute( // ✅ DO: Use SetAction with parseResult
            parseResult.GetValue(selfContainedSystemOption),
            parseResult.GetValue(verboseOption)
        ));
    }

    private static void Execute(string? solutionName, bool verbose)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet); // ✅ DO: Check prerequisites

        if (string.IsNullOrEmpty(solutionName))
        {
            AnsiConsole.MarkupLine("[red]Error: Solution name is required[/]");
            Environment.Exit(1); // ✅ DO: Exit on error
        }

        try
        {
            AnsiConsole.MarkupLine("[blue]Building solution...[/]"); // ✅ DO: Use AnsiConsole

            ProcessHelper.Run($"dotnet build {solutionName}", verbose); // ✅ DO: Use ProcessHelper.Run()
            AnsiConsole.MarkupLine("[green]Build completed successfully[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Environment.Exit(1);
        }
    }
}

public class BadBuildCommand : Command
{
    public BadBuildCommand() : base("bad-build", "Bad build command")
    {
        // ❌ Use inconsistent option naming (single dash for long names, double dash for short)
        AddOption(new Option<string?>(["-file-name", "--f"], "The name of the solution to process"));
        Handler = CommandHandler.Create<string>(Execute); // ❌ DON'T: Use CommandHandler.Create, use SetAction instead
    }
    private static int Execute(string file)
    {
        // ❌ DON'T: Skip prerequisite checks
        if (string.IsNullOrEmpty(file)) throw new ArgumentException("File required"); // ❌ DON'T: Throw exceptions
        var process = System.Diagnostics.Process.Start("dotnet", $"build {file}"); // ❌ DON'T: Use Process.Start directly
        process.WaitForExit();
        return process.ExitCode; // ❌ DON'T: Return exit code, use Environment.Exit instead
    }
}
```

## Troubleshooting

The CLI is self-compiling, so to build use `build(cli=true)`. Sometimes you will get errors like:

```bash
Failed to publish new CLI. Please run 'dotnet run' to fix. Could not load file or assembly 'System.IO.Pipelines,
Version=9.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'. The system cannot find the
```

Just retry the command and it should work.
