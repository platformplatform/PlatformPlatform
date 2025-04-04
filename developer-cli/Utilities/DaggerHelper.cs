using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class DaggerHelper
{
    /// <summary>
    /// Gets the content for the main dagger.cue file
    /// </summary>
    public static string GetMainDaggerContent()
    {
        return """
               package main

               import (
                   "dagger.io/dagger"
               )

               // Common configuration for all pipelines
               #Config: {
                   // Source code
                   source: dagger.#FS

                   // Environment variables
                   env: [string]: string

                   // Local-only mode skips steps requiring cloud access
                   localOnly: bool | *false
               }

               // Common actions
               actions: {}
               """;
    }

    /// <summary>
    /// Generates a CUE file for a specific workflow
    /// </summary>
    public static string GenerateWorkflowCueFile(string workflowName, string workflowYaml)
    {
        // Parse the YAML into a dictionary
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var workflowDict = deserializer.Deserialize<Dictionary<string, object>>(workflowYaml);

        // Extract jobs
        var jobs = workflowDict.TryGetValue("jobs", out var jobsObj)
            ? jobsObj as Dictionary<object, object>
            : new Dictionary<object, object>();

        // Generate the CUE content
        var sb = new StringBuilder();
        sb.AppendLine($"package {SanitizePackageName(workflowName)}");
        sb.AppendLine();
        sb.AppendLine("import (");
        sb.AppendLine("    \"dagger.io/dagger\"");
        sb.AppendLine(")");
        sb.AppendLine();

        // Generate workflow configuration
        sb.AppendLine($"// Configuration for {workflowName} workflow");
        sb.AppendLine($"#{workflowName}Config: {{");
        sb.AppendLine("    config: #Config");
        sb.AppendLine("}");
        sb.AppendLine();

        // Generate jobs
        var jobCount = 0;
        foreach (var job in jobs!)
        {
            var jobName = job.Key.ToString()!;
            var jobDetails = job.Value as Dictionary<object, object>;

            // Skip jobs that are uses (reusable workflows)
            if (jobDetails!.ContainsKey("uses")) continue;

            // Generate job
            GenerateJob(sb, workflowName, jobName, jobDetails!, jobCount++);
        }

        // Generate main actions
        sb.AppendLine("// Main actions");
        sb.AppendLine($"{workflowName}-check: {{");

        // For each job, add a run step
        for (var i = 0; i < jobCount; i++)
        {
            sb.AppendLine($"    job{i}: {workflowName}-job{i}");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        // Generate individual job actions
        for (var i = 0; i < jobCount; i++)
        {
            sb.AppendLine($"{workflowName}-job{i}: #{workflowName}Job{i} & {{");
            sb.AppendLine($"    config: #{workflowName}Config & {{");
            sb.AppendLine($"        config: #Config & {{");
            sb.AppendLine($"            source: client.filesystem.\".\"");
            sb.AppendLine($"            env: client.env");
            sb.AppendLine($"            localOnly: client.env.LOCAL_ONLY == \"true\"");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");
        }

        return sb.ToString();
    }

    private static void GenerateJob(StringBuilder sb, string workflowName, string jobName, Dictionary<object, object> jobDetails, int jobIndex)
    {
        sb.AppendLine($"// Job: {jobName}");
        sb.AppendLine($"#{workflowName}Job{jobIndex}: {{");
        sb.AppendLine($"    config: #{workflowName}Config");

        // Container settings
        sb.AppendLine("    container: {");
        sb.AppendLine("        _image: alpine:latest");
        sb.AppendLine("        _packages: [\"bash\", \"curl\", \"git\", \"nodejs\", \"npm\", \"openssl\"]");

        // Add .NET if needed
        if (HasDotNetSteps(jobDetails))
        {
            sb.AppendLine("        _dotnetPackages: [\"dotnet9-sdk\", \"icu-libs\"]");
        }

        sb.AppendLine("        exec: [\"apk\", \"add\"] + _packages");

        // Add .NET if needed
        if (HasDotNetSteps(jobDetails))
        {
            sb.AppendLine("        exec: [\"apk\", \"add\"] + _dotnetPackages");
        }

        sb.AppendLine("    }");

        // Mount source code
        sb.AppendLine("    directory: {");
        sb.AppendLine("        _src: config.config.source");
        sb.AppendLine("        path: \"/src\"");
        sb.AppendLine("        contents: _src");
        sb.AppendLine("    }");

        // Generate steps
        sb.AppendLine("    steps: {");

        // Add generated steps based on the job
        if (jobDetails.TryGetValue("steps", out var stepsObj))
        {
            var steps = stepsObj as List<object>;
            var stepIndex = 0;
            foreach (var step in steps!)
            {
                var stepDict = step as Dictionary<object, object>;
                GenerateStep(sb, stepDict!, stepIndex++);
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateStep(StringBuilder sb, Dictionary<object, object> step, int stepIndex)
    {
        // Skip uses steps for now
        if (step.ContainsKey("uses")) return;

        // Get step name or default name
        var name = step.TryGetValue("name", out var nameObj)
            ? nameObj.ToString()
            : "unnamed";

        // Get run command if it exists
        if (step.TryGetValue("run", out var runObj))
        {
            var runCommand = runObj.ToString();

            sb.AppendLine($"        step{stepIndex}: {{");
            sb.AppendLine($"            name: \"{SanitizeStepName(name!)}\"");

            // If working-directory is specified, use it
            if (step.TryGetValue("working-directory", out var workingDirObj))
            {
                var workingDir = workingDirObj.ToString();
                sb.AppendLine($"            workdir: \"/src/{workingDir}\"");
            }
            else
            {
                sb.AppendLine($"            workdir: \"/src\"");
            }

            // Escape any ${{ sequences in GitHub Actions
            runCommand = runCommand!.Replace("${{", "\\${{");

            // Add LOCAL_ONLY check if appropriate
            if (runCommand.Contains("npm ci") || runCommand.Contains("docker") ||
                runCommand.Contains("az ") || runCommand.Contains("gh "))
            {
                runCommand = $"if [ \"$LOCAL_ONLY\" = \"true\" ]; then\n" +
                             $"                echo \"[LOCAL MODE] Skipping: {runCommand!.Replace("\n", " ")}\"\n" +
                             $"            else\n" +
                             $"                {runCommand}\n" +
                             $"            fi";
            }

            // Generate command
            sb.AppendLine("            exec: {");
            sb.AppendLine("                command: \"bash\"");
            sb.AppendLine("                args: [\"-c\", #\"\"\"");
            sb.AppendLine($"                    {runCommand}");
            sb.AppendLine("                \"\"\"]");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
        }
    }

    private static bool HasDotNetSteps(Dictionary<object, object> jobDetails)
    {
        if (jobDetails.TryGetValue("steps", out var stepsObj))
        {
            var steps = stepsObj as List<object>;
            foreach (var step in steps!)
            {
                var stepDict = step as Dictionary<object, object>;

                // Check if the step uses the dotnet setup action
                if (stepDict!.TryGetValue("uses", out var usesObj))
                {
                    var uses = usesObj.ToString();
                    if (uses!.Contains("dotnet") || uses.Contains("setup-dotnet"))
                    {
                        return true;
                    }
                }

                // Check if the step runs dotnet commands
                if (stepDict.TryGetValue("run", out var runObj))
                {
                    var run = runObj.ToString();
                    if (run!.Contains("dotnet "))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string SanitizePackageName(string name)
    {
        // Replace hyphens with underscores
        var sanitized = name.Replace("-", "_");

        // Remove any characters that aren't alphanumeric or underscore
        sanitized = Regex.Replace(sanitized, "[^a-zA-Z0-9_]", "");

        return sanitized;
    }

    private static string SanitizeStepName(string name)
    {
        // Replace any non-alphanumeric characters with underscores and convert to lowercase
        return Regex.Replace(name, "[^a-zA-Z0-9]", "_").ToLowerInvariant();
    }
}
