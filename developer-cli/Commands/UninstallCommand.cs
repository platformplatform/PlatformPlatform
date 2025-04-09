using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class UninstallCommand : Command
{
    public UninstallCommand() : base("uninstall", $"Will remove the {Configuration.AliasName} CLI alias")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private void Execute()
    {
        if (Configuration.IsWindows && !Configuration.IsDebugMode)
        {
            AnsiConsole.MarkupLine($"[yellow]Please run 'dotnet run uninstall' from {Configuration.CliFolder}.[/]");
            Environment.Exit(0);
        }

        var prompt =
            $"""
             Confirm uninstallation:

             This will do the following:
             - Remove the PlatformPlatform Developer CLI alias (on Mac) and remove the CLI from the PATH (Windows)
             - Delete the {Configuration.PublishFolder}/{Configuration.AliasName}.* files
             - Remove the {Configuration.PublishFolder} folder if empty

             Are you sure you want to uninstall the PlatformPlatform Developer CLI?
             """;

        if (AnsiConsole.Confirm(prompt))
        {
            DeleteFilesFolder();
            RemoveAlias();

            AnsiConsole.MarkupLine("[green]Please restart your terminal.[/]");
        }
    }

    private void RemoveAlias()
    {
        if (Configuration.IsWindows)
        {
            // Only remove the folder from the PATH if it exists... it might be used by other PlatformPlatform CLIs running side by side
            if (!Directory.Exists(Configuration.PublishFolder))
            {
                Configuration.Windows.RemoveFolderFromPath(Configuration.PublishFolder);
                AnsiConsole.MarkupLine("[green]The PlatformPlatform CLI folder has been removed from the PATH.[/]");
            }
        }
        else if (Configuration.IsMacOs || Configuration.IsLinux)
        {
            Configuration.MacOs.DeleteAlias();
            AnsiConsole.MarkupLine("[green]Alias has been removed.[/]");
        }
    }

    private void DeleteFilesFolder()
    {
        if (!Directory.Exists(Configuration.PublishFolder)) return;

        // Multiple CLIs can be running side by side. Only delete the files belonging to the current version.
        foreach (var file in Directory.GetFiles(Configuration.PublishFolder, $"{Configuration.AliasName}.*"))
        {
            File.Delete(file);
        }

        // Delete the Configuration.PublishFolder if empty
        if (!Directory.EnumerateFileSystemEntries(Configuration.PublishFolder).Any())
        {
            Directory.Delete(Configuration.PublishFolder);
        }
    }
}
