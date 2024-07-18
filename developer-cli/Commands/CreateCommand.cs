using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public partial class CreateCommand : Command
{
    public CreateCommand() : base("create", "Create a new project")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private void Execute()
    {
        var applicationDirectory = Path.GetFullPath(Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application"));
        var mainPackageJsonFile = Path.Combine(applicationDirectory, "package.json");
        var mainSolutionFile = "PlatformPlatform.sln";

        AnsiConsole.MarkupLine("Enter the name of the new project below using Pascal case (e.g. `MyNewProject`):");
        var projectNamePascalCase = AnsiConsole.Ask<string>("Name: ");
        var projectNameCamelCase = projectNamePascalCase.Substring(0, 1).ToLower() + projectNamePascalCase.Substring(1);
        var projectNameKebabCase = Regex.Replace(projectNamePascalCase, "([a-z])([A-Z])", "$1-$2").ToLower();
        var projectNamePascalCaseSpaces = Regex.Replace(projectNamePascalCase, "([a-z])([A-Z])", "$1 $2");
        var projectNameSnakeCase = Regex.Replace(projectNamePascalCase, "([a-z])([A-Z])", "$1_$2").ToUpper();

        if (ProjectNameRegex().IsMatch(projectNamePascalCase) == false)
        {
            AnsiConsole.MarkupLine("[red]Project name must be in Pascal case (e.g. `MyNewProject`)[/]");
            return;
        }

        if (projectNamePascalCase is "AppHost" or "AppGateway")
        {
            AnsiConsole.MarkupLine("[red]Project name cannot be `AppHost` or `AppGateway`[/]");
            return;
        }

        if (projectNameKebabCase.StartsWith("shared-"))
        {
            AnsiConsole.MarkupLine("[red]Project name cannot start with `shared-`[/]");
            return;
        }

        var projectDirectory = Path.Combine(applicationDirectory, projectNameKebabCase);
        var templateDirectory = Path.Combine(applicationDirectory, "back-office");

        if (Directory.Exists(projectDirectory))
        {
            AnsiConsole.MarkupLine($"[red]Project {projectNamePascalCase} folder already exists {projectDirectory}[/]");
            return;
        }

        // Find all projects in the application folder containing a sub folder named "Api"
        var projects = Directory.GetDirectories(applicationDirectory, "*", SearchOption.TopDirectoryOnly)
            .Where(d => Directory.Exists(Path.Combine(d, "Api")))
            .Select(d => Path.GetFileName(d))
            .ToList();

        var defaultPort = 9000 + projects.Count * 100;

        // Get user input
        var mainPort = AnsiConsole.Ask($"Enter the main port for the new project (default: {defaultPort}): ", defaultPort);

        var projectItemList = new Dictionary<string, string>
        {
            // { $"{projectNameKebabCase}", Path.Join($"{projectNameKebabCase}") },
            { $"{projectNamePascalCase}.Domain", Path.Join($"{projectNameKebabCase}", "Domain", $"{projectNamePascalCase}.Domain.csproj") },
            { $"{projectNamePascalCase}.Application", Path.Join($"{projectNameKebabCase}", "Application", $"{projectNamePascalCase}.Application.csproj") },
            { $"{projectNamePascalCase}.Infrastructure", Path.Join($"{projectNameKebabCase}", "Infrastructure", $"{projectNamePascalCase}.Infrastructure.csproj") },
            { $"{projectNamePascalCase}.Workers", Path.Join($"{projectNameKebabCase}", "Workers", $"{projectNamePascalCase}.Workers.csproj") },
            { $"{projectNamePascalCase}.Api", Path.Join($"{projectNameKebabCase}", "Api", $"{projectNamePascalCase}.Api.csproj") },
            { $"{projectNamePascalCase}.WebApp", Path.Join($"{projectNameKebabCase}", "WebApp", $"{projectNamePascalCase}.WebApp.esproj") },
            { $"{projectNamePascalCase}.Tests", Path.Join($"{projectNameKebabCase}", "Tests", $"{projectNamePascalCase}.Tests.csproj") }
        };

        AnsiConsole.MarkupLine($"Creating project folder [green]{projectNameKebabCase}[/]...{templateDirectory} -> {projectDirectory}");

        var sourceFiles = Directory.GetFiles(templateDirectory, "*.*", SearchOption.AllDirectories);

        AnsiConsole.MarkupLine($"[green]Creating project files for {projectNamePascalCase}[/]");

        foreach (var file in sourceFiles)
        {
            if (ShouldSkipFile(file)) continue;

            var relativePath = Path.GetRelativePath(templateDirectory, file).Replace("BackOffice", projectNamePascalCase);
            var destFile = Path.Combine(projectDirectory, relativePath);

            var destDir = Path.GetDirectoryName(destFile) ?? throw new InvalidOperationException("Destination directory is null");

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Read file contents and replace `BackOffice` with the new project name
            var fileContents = File.ReadAllText(file, new UTF8Encoding());
            fileContents = fileContents.Replace("BackOffice", projectNamePascalCase);
            fileContents = fileContents.Replace("backOffice", projectNameCamelCase);
            fileContents = fileContents.Replace("back-office", projectNameKebabCase);

            if (file.EndsWith($"Api{Path.DirectorySeparatorChar}Program.cs"))
            {
                fileContents = fileContents.Replace("9200", mainPort.ToString());
            }

            if (file.EndsWith($"Workers{Path.DirectorySeparatorChar}Program.cs"))
            {
                fileContents = fileContents.Replace("9299", (mainPort + 99).ToString());
            }

            if (file.EndsWith("launchSettings.json") || file.EndsWith("BaseApiTest.cs") || file.EndsWith("rsbuild.config.ts"))
            {
                fileContents = fileContents.Replace("9201", (mainPort + 1).ToString());
            }

            if (file.EndsWith("index.html") || file.EndsWith("tsconfig.json") || file.EndsWith(".tsx"))
            {
                fileContents = fileContents.Replace("Back Office", projectNamePascalCaseSpaces);
            }

            File.WriteAllText(destFile, fileContents, new UTF8Encoding());
            AnsiConsole.MarkupLine($"Write file [green]{destFile}[/]");
        }

        AnsiConsole.MarkupLine($"[green]Adding WebApp workspace {projectNamePascalCase}[/]");
        var packageJsonContents = File.ReadAllText(mainPackageJsonFile, new UTF8Encoding());
        packageJsonContents = packageJsonContents.Replace("\"back-office/WebApp\",", $"\"back-office/WebApp\",\n    \"{projectNameKebabCase}/WebApp\",");
        File.WriteAllText(mainPackageJsonFile, packageJsonContents, new UTF8Encoding());

        AnsiConsole.MarkupLine("[green]Adding new projects AppGateway[/]");
        // "Routes": {
        var routeConfig = $@"
      ""{projectNameKebabCase}-api"": {{
        ""ClusterId"": ""{projectNameKebabCase}-api"",
        ""Match"": {{
          ""Path"": ""/api/{projectNameKebabCase}/{{**catch-all}}""
        }},
        ""Transforms"": [
          {{
            ""PathPattern"": ""api/{{**catch-all}}""
          }}
        ]
      }},
      ""{projectNameKebabCase}-spa"": {{
        ""ClusterId"": ""{projectNameKebabCase}-api"",
        ""Match"": {{
          ""Path"": ""/{projectNameKebabCase}/{{**catch-all}}""
        }},
        ""Transforms"": [
          {{
            ""PathPattern"": ""{{**catch-all}}""
          }}
        ]
      }},";
        //     "Clusters": {{
        var clusterConfig = $@"
    ""{projectNameKebabCase}-api"": {{
      ""Destinations"": {{
        ""destination"": {{
          ""Address"": ""https://localhost:{mainPort}""
        }}
      }}
    }},";

        var gatewayConfigFile = Path.Combine(applicationDirectory, "AppGateway", "appsettings.json");
        var gatewayConfigContents = File.ReadAllText(gatewayConfigFile, new UTF8Encoding());
        gatewayConfigContents = gatewayConfigContents.Replace("\"Routes\": {", $"\"Routes\": {{{routeConfig}");
        gatewayConfigContents = gatewayConfigContents.Replace("\"Clusters\": {", $"\"Clusters\": {{{clusterConfig}");
        File.WriteAllText(gatewayConfigFile, gatewayConfigContents, new UTF8Encoding());

        var clusterConfigFilter = $"\"{projectNameKebabCase}-api\" => ReplaceDestinationAddress(cluster, \"{projectNameSnakeCase.ToUpper()}_API_URL\"),";
        var clusterDestinationConfigFilter = Path.Combine(applicationDirectory, "AppGateway", "Filters", "ClusterDestinationConfigFilter.cs");
        var clusterDestinationConfigFilterContents = File.ReadAllText(clusterDestinationConfigFilter, new UTF8Encoding());
        clusterDestinationConfigFilterContents = clusterDestinationConfigFilterContents.Replace("\"back-office-api\"", $"{clusterConfigFilter}\n            \"back-office-api\"");
        File.WriteAllText(clusterDestinationConfigFilter, clusterDestinationConfigFilterContents, new UTF8Encoding());

        AnsiConsole.MarkupLine($"[green]Adding {projectNamePascalCase} project reference in AppHost[/]");

        ProcessHelper.StartProcess($"dotnet add AppHost/AppHost.csproj reference {Path.Join(projectNameKebabCase, "Api", $"{projectNamePascalCase}.Api.csproj")}", applicationDirectory, waitForExit: true);
        ProcessHelper.StartProcess($"dotnet add AppHost/AppHost.csproj reference {Path.Join(projectNameKebabCase, "WebApp", $"{projectNamePascalCase}.WebApp.esproj")}", applicationDirectory, waitForExit: true);
        ProcessHelper.StartProcess($"dotnet add AppHost/AppHost.csproj reference {Path.Join(projectNameKebabCase, "Workers", $"{projectNamePascalCase}.Workers.csproj")}", applicationDirectory, waitForExit: true);


        AnsiConsole.MarkupLine("[green]Adding new projects to AppHost[/]");
        var appHostProgram = $@"var {projectNameCamelCase}Database = sqlServer
    .AddDatabase(""{projectNameKebabCase}-database"", ""{projectNameKebabCase}"");

var {projectNameCamelCase}Api = builder
    .AddProject<{projectNamePascalCase}_Api>(""{projectNameKebabCase}-api"")
    .WithReference({projectNameCamelCase}Database)
    .WithReference(azureStorage);

builder
    .AddProject<{projectNamePascalCase}_Workers>(""{projectNameKebabCase}-workers"")
    .WithReference({projectNameCamelCase}Database)
    .WithReference(azureStorage);
";

        var appHostProgramReference = $".WithReference({projectNameCamelCase}Api)\n    .WithReference(backOfficeApi);";

        var appHostProgramFile = Path.Combine(applicationDirectory, "AppHost", "Program.cs");
        var appHostProgramContents = File.ReadAllText(appHostProgramFile, new UTF8Encoding());
        appHostProgramContents = appHostProgramContents.Replace("var backOfficeDatabase = sqlServer", $"{appHostProgram}\nvar backOfficeDatabase = sqlServer");
        appHostProgramContents = appHostProgramContents.Replace(".WithReference(backOfficeApi);", appHostProgramReference);
        File.WriteAllText(appHostProgramFile, appHostProgramContents, new UTF8Encoding());

        AnsiConsole.MarkupLine($"[green]Adding {projectNamePascalCase} projects to solution[/]");
        foreach (var projectItem in projectItemList)
        {
            // Add the new project to the solution file
            var addProjectCommand = $"dotnet sln {mainSolutionFile} add {projectItem.Value}";
            AnsiConsole.MarkupLine($"[green]Adding project {projectItem.Key} to solution[/]");
            ProcessHelper.StartProcess(addProjectCommand, applicationDirectory, waitForExit: true);
        }

        AnsiConsole.MarkupLine("[green]Restoring npm packages for the new project[/]");
        ProcessHelper.StartProcess("npm install", applicationDirectory, waitForExit: true);

        AnsiConsole.MarkupLine("[green]Building the new project WebApp[/]");
        ProcessHelper.StartProcess("npm run build", applicationDirectory, waitForExit: true);


        AnsiConsole.MarkupLine("[green]Restoring .Net packages for the new project[/]");
        ProcessHelper.StartProcess("dotnet restore", applicationDirectory, waitForExit: true);

        AnsiConsole.MarkupLine("[green]Building the new project[/]");
        ProcessHelper.StartProcess("dotnet build PlatformPlatform.sln --no-restore", applicationDirectory, waitForExit: true);

        AnsiConsole.MarkupLine($"[green]Project {projectNamePascalCase} created successfully at {projectDirectory}[/]");
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$")]
    private static partial Regex ProjectNameRegex();

    private static bool ShouldSkipFile(string filePath)
    {
        foreach (var folderName in new[] { "node_modules", "artifacts", "obj" })
        {
            var folderPath = $"{Path.DirectorySeparatorChar}{folderName}{Path.DirectorySeparatorChar}";
            if (filePath.Contains(folderPath))
            {
                return true;
            }
        }

        return false;
    }
}
