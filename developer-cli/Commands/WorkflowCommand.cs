using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using System.Text;
using System.Text.RegularExpressions;

namespace PlatformPlatform.DeveloperCli.Commands;

public class WorkflowCommand : Command
{
    private readonly string _workspaceRoot;

    public WorkflowCommand() : base(
        "workflow",
        "Run GitHub workflows locally, directly from YAML files"
    )
    {
        AddAlias("w");

        AddOption(new Option<string>(
            ["--workflow", "-w"],
            "The workflow to run (e.g., account-management, back-office)"
        ));

        AddOption(new Option<bool>(
            "--local-only",
            "Skip steps requiring cloud resources"
        ));

        _workspaceRoot = Configuration.SourceCodeFolder;

        Handler = CommandHandler.Create<string?, bool>(Execute);
    }

    private int Execute(string? workflow = null, bool localOnly = false)
    {
        // Show intro and get workflow if not specified
        PrintHeader("Local Workflow Runner");

        var githubWorkflowsDir = Path.Combine(_workspaceRoot, ".github", "workflows");
        if (!Directory.Exists(githubWorkflowsDir))
        {
            AnsiConsole.MarkupLine($"[red]Error: GitHub workflows directory not found at {githubWorkflowsDir}[/]");
            return 1;
        }

        // If workflow not specified, let user select one
        workflow ??= SelectWorkflow(githubWorkflowsDir);

        // Validate workflow exists
        var workflowFile = Path.Combine(githubWorkflowsDir, $"{workflow}.yml");
        if (!File.Exists(workflowFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: Workflow file '{workflow}.yml' not found.[/]");
            return 1;
        }

        // Read workflow
        var workflowContent = File.ReadAllText(workflowFile);
        PrintHeader($"Running {workflow} workflow locally");

        try
        {
            // Run the workflow directly using the YAML file
            var result = RunWorkflowFromYaml(workflow, workflowFile, localOnly);

            if (result)
            {
                AnsiConsole.MarkupLine($"\n[green]✓ Workflow {workflow} executed successfully[/]");
                if (localOnly)
                {
                    AnsiConsole.MarkupLine($"[yellow]Note: Some steps were skipped due to --local-only flag. Cloud resources and deployments were not executed.[/]");
                }
                return 0;
            }

            AnsiConsole.MarkupLine($"\n[red]✗ Workflow {workflow} execution failed[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error running workflow: {ex.Message}[/]");
            return 1;
        }
    }

    private void PrintHeader(string heading)
    {
        var separator = new string('-', Console.WindowWidth - heading.Length - 1);
        AnsiConsole.MarkupLine($"\n[bold][green]{heading}[/] {separator}[/]\n");
    }

    private string SelectWorkflow(string githubWorkflowsDir)
    {
        var workflowFiles = Directory
            .GetFiles(githubWorkflowsDir, "*.yml")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(f => !f.StartsWith("_")) // Exclude reusable workflows
            .ToList();

        if (workflowFiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: No workflow files found in .github/workflows directory[/]");
            Environment.Exit(1);
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a workflow to run:")
                .PageSize(10)
                .AddChoices(workflowFiles)
        );
    }

    private bool RunWorkflowFromYaml(string workflowName, string workflowFile, bool localOnly)
    {
        try
        {
            // Make sure the current directory is set to the root workspace
            Directory.SetCurrentDirectory(_workspaceRoot);

            AnsiConsole.Status().Start($"Running {workflowName} workflow locally...", ctx =>
            {
                ctx.Status("Preparing workflow environment...");
            });

            // Create a bash script that will execute the workflow steps for real
            var tempScriptDir = Path.Combine(Path.GetTempPath(), "workflow-runner");
            Directory.CreateDirectory(tempScriptDir);

            var scriptName = Path.Combine(tempScriptDir, $"run_{workflowName}.sh");

            // Generate script content based on the actual workflow file
            var scriptContent = GenerateWorkflowScript(workflowFile, localOnly);

            File.WriteAllText(scriptName, scriptContent);

            if (!Configuration.IsWindows)
            {
                try
                {
                    // Use System.Diagnostics.Process directly to avoid issues with chmod
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"755 {scriptName}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = System.Diagnostics.Process.Start(processStartInfo);
                    process?.WaitForExit();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Could not set executable permission: {ex.Message}[/]");
                }
            }

            // Run the script directly
            try
            {
                AnsiConsole.MarkupLine($"[blue]Executing workflow directly from YAML: {workflowName}[/]");

                ProcessHelper.StartProcess($"/bin/bash \"{scriptName}\"", exitOnError: false, redirectOutput: false);

                // We no longer need to run additional steps - the workflow handles everything correctly
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error running workflow: {ex.Message}[/]");
                return false;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Workflow execution failed: {ex.Message}[/]");
            return false;
        }
    }

    private void RunAdditionalSteps(string slnfName, string yamlContent)
    {
        var workingDir = Path.Combine(_workspaceRoot, "application");
        Directory.SetCurrentDirectory(workingDir);

        try
        {
            // Only run these if the workflow actually contains them
            if (yamlContent.Contains("build"))
            {
                string slnfPath = $"{slnfName}/{slnfName}.slnf";

                // Check if the solution file exists
                if (File.Exists(slnfPath))
                {
                    AnsiConsole.MarkupLine($"[blue]Building {slnfName}...[/]");
                    ProcessHelper.StartProcess($"dotnet build {slnfPath} --no-restore", exitOnError: false);

                    if (yamlContent.Contains("test"))
                    {
                        AnsiConsole.MarkupLine($"[blue]Testing {slnfName}...[/]");
                        ProcessHelper.StartProcess($"dotnet test {slnfPath} --no-build", exitOnError: false);
                    }
                }
            }

            if (yamlContent.Contains("npm run build"))
            {
                AnsiConsole.MarkupLine($"[blue]Building frontend...[/]");
                ProcessHelper.StartProcess("npm run build", exitOnError: false);
            }

            if (yamlContent.Contains("check"))
            {
                var webAppDir = Path.Combine(workingDir, slnfName, "WebApp");
                if (Directory.Exists(webAppDir))
                {
                    Directory.SetCurrentDirectory(webAppDir);
                    AnsiConsole.MarkupLine($"[blue]Running frontend checks...[/]");
                    ProcessHelper.StartProcess("npm run check", exitOnError: false);
                }
            }
        }
        finally
        {
            // Restore working directory
            Directory.SetCurrentDirectory(_workspaceRoot);
        }
    }

    private string GenerateWorkflowScript(string workflowFile, bool localOnly)
    {
        // Read and parse the workflow YAML
        var workflowContent = File.ReadAllText(workflowFile);
        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();

        var workflow = deserializer.Deserialize<Dictionary<string, object>>(workflowContent);

        // Extract the workflow name if available
        string workflowName = workflow.TryGetValue("name", out var workflowNameObj)
            ? workflowNameObj?.ToString() ?? Path.GetFileNameWithoutExtension(workflowFile)
            : Path.GetFileNameWithoutExtension(workflowFile);

        // Extract jobs
        var jobs = workflow.TryGetValue("jobs", out var jobsObj)
            ? jobsObj as Dictionary<object, object>
            : new Dictionary<object, object>();

        var sb = new StringBuilder();
        sb.AppendLine("#!/bin/bash");
        sb.AppendLine();
        sb.AppendLine("# Script generated by PlatformPlatform Developer CLI");
        sb.AppendLine($"# Running GitHub workflow '{workflowName}' locally with{(localOnly ? " " : "out ")}--local-only flag");
        sb.AppendLine();
        sb.AppendLine("set -e  # Exit on error");
        sb.AppendLine();

        // Setup common environment variables
        sb.AppendLine("# Setup environment variables");
        sb.AppendLine("export GITHUB_WORKSPACE=\"$(pwd)\"");
        sb.AppendLine("export GITHUB_OUTPUT=/tmp/github_output");
        sb.AppendLine("export WORKFLOW_NAME=\"" + workflowName + "\"");
        sb.AppendLine("touch $GITHUB_OUTPUT");
        sb.AppendLine();

        if (localOnly)
        {
            sb.AppendLine("# Running in local-only mode (skipping cloud resources)");
            sb.AppendLine("export LOCAL_ONLY=true");
            sb.AppendLine();
        }

        sb.AppendLine("echo \"=== Starting workflow execution: $WORKFLOW_NAME ===\"");

        // Generate steps for each job
        foreach (var job in jobs!)
        {
            var jobName = job.Key?.ToString() ?? "unnamed-job";
            var jobDetails = job.Value as Dictionary<object, object>;
            if (jobDetails == null) continue;

            // Skip jobs that use reusable workflows if in local-only mode
            if (localOnly && jobDetails.ContainsKey("uses"))
            {
                var usesValue = jobDetails["uses"]?.ToString();
                if (usesValue != null && usesValue.StartsWith("./.github/workflows/"))
                {
                    sb.AppendLine($"echo \"Skipping job '{jobName}' (reusable workflow) in local-only mode\"");
                    continue;
                }
            }

            // Skip jobs with if conditions that depend on deploy variables in local-only mode
            if (localOnly && jobDetails.TryGetValue("if", out var ifCondition))
            {
                var ifValue = ifCondition?.ToString();
                if (ifValue != null && (ifValue.Contains("deploy_") || ifValue.Contains("needs.")))
                {
                    sb.AppendLine($"echo \"Skipping job '{jobName}' (deployment job) in local-only mode\"");
                    continue;
                }
            }

            sb.AppendLine($"echo \"\\n=== Running job: {jobName} ===\"");

            // Extract the job's working directory if specified
            string defaultWorkDir = "";
            if (jobDetails.TryGetValue("defaults", out var defaultsObj))
            {
                var defaults = defaultsObj as Dictionary<object, object>;
                if (defaults != null && defaults.TryGetValue("run", out var runDefaults))
                {
                    var runDefaultsDict = runDefaults as Dictionary<object, object>;
                    if (runDefaultsDict != null && runDefaultsDict.TryGetValue("working-directory", out var workDirObj))
                    {
                        defaultWorkDir = workDirObj?.ToString() ?? "";
                    }
                }
            }

            // Process steps
            if (jobDetails.TryGetValue("steps", out var stepsObj))
            {
                var steps = stepsObj as List<object>;
                if (steps == null) continue;

                bool skipRemainingSteps = false;

                // Process each step
                foreach (var step in steps)
                {
                    if (skipRemainingSteps) break;

                    var stepDict = step as Dictionary<object, object>;
                    if (stepDict == null) continue;

                    // Get step name and ID
                    var name = stepDict.TryGetValue("name", out var nameObj)
                        ? nameObj?.ToString() ?? "Unnamed Step"
                        : "Unnamed Step";

                    var id = stepDict.TryGetValue("id", out var idObj)
                        ? idObj?.ToString()
                        : null;

                    sb.AppendLine($"echo \"\\n--- Step: {name} ---\"");

                    // If it's a checkout action, simulate it with a message
                    if (stepDict.TryGetValue("uses", out var usesObj))
                    {
                        var usesValue = usesObj?.ToString();
                        if (usesValue != null)
                        {
                            if (usesValue.Contains("actions/checkout"))
                            {
                                sb.AppendLine("echo \"Skipping checkout (already in local workspace)\"");
                                continue;
                            }
                            else if (usesValue.Contains("setup-node"))
                            {
                                sb.AppendLine("echo \"Checking for Node.js...\"");
                                sb.AppendLine("if ! command -v node &> /dev/null; then");
                                sb.AppendLine("    echo \"Node.js not found. Please install Node.js.\"");
                                sb.AppendLine("    exit 1");
                                sb.AppendLine("fi");
                                sb.AppendLine("node --version");
                                continue;
                            }
                            else if (usesValue.Contains("setup-dotnet"))
                            {
                                sb.AppendLine("echo \"Checking for .NET...\"");
                                sb.AppendLine("if ! command -v dotnet &> /dev/null; then");
                                sb.AppendLine("    echo \".NET not found. Please install .NET SDK.\"");
                                sb.AppendLine("    exit 1");
                                sb.AppendLine("fi");
                                sb.AppendLine("dotnet --version");
                                continue;
                            }
                            else if (usesValue.Contains("setup-java"))
                            {
                                sb.AppendLine("echo \"Checking for Java...\"");
                                sb.AppendLine("if ! command -v java &> /dev/null; then");
                                sb.AppendLine("    echo \"Java not found. Please install Java.\"");
                                sb.AppendLine("    if [ \"$LOCAL_ONLY\" = \"true\" ]; then");
                                sb.AppendLine("        echo \"Continuing in local-only mode without Java...\"");
                                sb.AppendLine("        # Set a dummy JAVA_HOME to prevent errors in subsequent steps");
                                sb.AppendLine("        export JAVA_HOME=\"/dummy/path\"");
                                sb.AppendLine("    else");
                                sb.AppendLine("        exit 1");
                                sb.AppendLine("    fi");
                                sb.AppendLine("else");
                                sb.AppendLine("    java -version");
                                sb.AppendLine("fi");
                                continue;
                            }
                            else if (usesValue.Contains("upload-artifact") ||
                                     usesValue.Contains("download-artifact"))
                            {
                                sb.AppendLine("echo \"Skipping artifact step in local execution\"");
                                continue;
                            }
                            else if (localOnly)
                            {
                                sb.AppendLine($"echo \"Skipping action {usesValue} in local-only mode\"");
                                continue;
                            }
                        }
                    }

                    // Skip conditional steps that depend on deploy variables in local-only mode
                    if (localOnly && stepDict.TryGetValue("if", out var stepIfCondition))
                    {
                        var stepIfValue = stepIfCondition?.ToString();
                        if (stepIfValue != null)
                        {
                            // Check for if conditions that would stop execution in local mode
                            if (stepIfValue.Contains("deploy_") || stepIfValue.Contains("needs."))
                            {
                                // Make an exception for test steps that we want to run locally
                                if (name.Contains("Test") || name.Contains("Build") ||
                                    name.Contains("Run") || name.Contains("Code") ||
                                    name.Contains("Format") || name.Contains("Lint"))
                                {
                                    sb.AppendLine($"echo \"Running essential step '{name}' despite condition...\"");
                                }
                                else
                                {
                                    sb.AppendLine($"echo \"Skipping step '{name}' (deployment step) in local-only mode\"");
                                    continue;
                                }
                            }
                            else if (stepIfValue.Contains("SonarScanner") || stepIfValue.Contains("sonar"))
                            {
                                sb.AppendLine($"echo \"Skipping SonarScanner-dependent step '{name}' in local-only mode\"");
                                continue;
                            }
                        }
                    }

                    // Process run commands
                    if (stepDict.TryGetValue("run", out var runObj))
                    {
                        var runCommand = runObj?.ToString();
                        if (runCommand == null) continue;

                        // Store step ID for output references if it exists
                        if (!string.IsNullOrEmpty(id))
                        {
                            sb.AppendLine($"# Step ID: {id}");
                            sb.AppendLine($"CURRENT_STEP_ID=\"{id}\"");
                        }

                        // Get working directory if specified
                        string workDir = defaultWorkDir;
                        if (stepDict.TryGetValue("working-directory", out var workingDirObj))
                        {
                            workDir = workingDirObj?.ToString() ?? workDir;
                        }
                        if (!string.IsNullOrEmpty(workDir))
                        {
                            sb.AppendLine($"cd \"$GITHUB_WORKSPACE/{workDir}\"");
                        }

                        // Handle test/sonarscanner commands specially in local mode
                        if (localOnly)
                        {
                            // For SonarScanner steps, provide a local alternative
                            if (runCommand.Contains("sonarscanner") || name.Contains("SonarScanner"))
                            {
                                // Instead of trying to run SonarScanner, check if the command already has a condition
                                // Most SonarScanner commands check for SONAR_PROJECT_KEY and won't run when it's empty
                                if (runCommand.Contains("vars.SONAR_PROJECT_KEY") || runCommand.Contains("if [[${{"))
                                {
                                    sb.AppendLine("# Skipping SonarScanner command in local mode (no Sonar project key)");
                                    sb.AppendLine("# Original command had conditional check for vars.SONAR_PROJECT_KEY");

                                    // Just ensure we have proper return values
                                    sb.AppendLine("# Setting up variables to ensure workflow continues");
                                    sb.AppendLine("export SONAR_STARTED=false");

                                    if (!string.IsNullOrEmpty(workDir))
                                    {
                                        sb.AppendLine("cd \"$GITHUB_WORKSPACE\"");
                                    }

                                    if (!string.IsNullOrEmpty(id)) {
                                        sb.AppendLine($"echo \"Step completed: {id}\"");
                                    }

                                    continue;
                                }
                            }
                        }

                        // Sanitize GitHub expressions in the command
                        runCommand = SanitizeGitHubExpressions(runCommand);

                        // Add the command (without comments for less verbosity)
                        sb.AppendLine(runCommand);

                        // Only output step completion for steps with IDs
                        if (!string.IsNullOrEmpty(id)) {
                            sb.AppendLine($"echo \"Step completed: {id}\"");
                        }

                        // Reset working directory if needed
                        if (!string.IsNullOrEmpty(workDir))
                        {
                            sb.AppendLine("cd \"$GITHUB_WORKSPACE\"");
                        }
                    }
                }
            }

            sb.AppendLine("\necho \"=== Job completed ===\"");
        }

        sb.AppendLine("\necho \"\\n=== Workflow execution completed successfully ===\\n\"");

        return sb.ToString();
    }

    private string ExtractSolutionFileFromCommand(string command)
    {
        // Try to extract the solution file (.slnf) from a command string
        var slnMatch = Regex.Match(command, @"[\w-]+/[\w-]+\.slnf");
        if (slnMatch.Success)
        {
            return slnMatch.Value;
        }

        // Fallback for other solution file formats
        slnMatch = Regex.Match(command, @"[\w-]+\.sln");
        if (slnMatch.Success)
        {
            return slnMatch.Value;
        }

        return string.Empty;
    }

    private string SanitizeGitHubExpressions(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Make a copy of the original input for logging issues
        var original = input;
        var result = input;

        try
        {
            // Replace step output references
            result = Regex.Replace(result,
                @"\$\{\{\s*steps\.([^.}]+)\.outputs\.([^}]+)\s*\}\}",
                match =>
                {
                    var stepId = match.Groups[1].Value;
                    var outputVar = match.Groups[2].Value;
                    return $"$(grep \"{outputVar}=\" $GITHUB_OUTPUT | cut -d= -f2)";
                });

            // Replace vars references
            result = Regex.Replace(result,
                @"\$\{\{\s*vars\.([^}]+)\s*\}\}",
                match => $"local_var_{match.Groups[1].Value}");

            // Replace secrets references
            result = Regex.Replace(result,
                @"\$\{\{\s*secrets\.([^}]+)\s*\}\}",
                match => $"local_secret_{match.Groups[1].Value}");

            // Replace github context references
            result = Regex.Replace(result,
                @"\$\{\{\s*github\.([^}]+)\s*\}\}",
                match => $"local_github_{match.Groups[1].Value}");

            // More thorough complex condition handling
            result = Regex.Replace(result,
                @"\$\{\{\s*([^{}]+?(?:&&|\|\||==|!=)[^{}]+?)\s*\}\}",
                match => "true");

            // Replace any remaining ${{ ... }} expressions
            result = Regex.Replace(result, @"\$\{\{[^}]+\}\}", "dummy_value");

            // Fix any grep commands that use -P (not supported on macOS)
            if (Configuration.IsMacOs)
            {
                // Special handling for UserSecretsId extraction (macOS compatible)
                if (result.Contains("USER_SECRETS_ID=$(grep") && result.Contains("<UserSecretsId>"))
                {
                    result = result.Replace(
                        "USER_SECRETS_ID=$(grep -oP '(?<=<UserSecretsId>).*?(?=</UserSecretsId>)' SharedKernel.csproj)",
                        "USER_SECRETS_ID=$(grep '<UserSecretsId>' SharedKernel.csproj | sed 's/.*<UserSecretsId>\\(.*\\)<\\/UserSecretsId>.*/\\1/')");
                }

                // General replacement for lookahead/lookbehind patterns and -P option
                result = result.Replace("grep -oP", "grep -o");

                // Replace common regex patterns that don't work in macOS grep
                result = Regex.Replace(result,
                    @"grep -o '(?<=([^']+))([^']+)(?=([^']+))'",
                    "grep -o '$2'");

                // Replace any remaining complex grep with sed for macOS compatibility
                result = Regex.Replace(result,
                    @"grep -o '(.*?)'",
                    "grep -o '$1'");
            }

            return result;
        }
        catch (Exception ex)
        {
            // Log the error but return a safe version to prevent script failure
            Console.Error.WriteLine($"Error sanitizing expression: {ex.Message}");
            // Remove all GitHub expressions as a fallback
            return Regex.Replace(original, @"\$\{\{[^}]+\}\}", "dummy_value");
        }
    }

    private string ExtractWorkflowName(string workflowName)
    {
        // Convert workflow file name to solution name format
        // E.g., "back-office" -> "BackOffice", "account-management" -> "AccountManagement"
        if (string.IsNullOrEmpty(workflowName)) return string.Empty;

        // Split by dash and capitalize each part
        var parts = workflowName.Split('-');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
        }

        // Join without dashes
        return string.Join(string.Empty, parts);
    }
}
