using System.Text;
using System.Text.RegularExpressions;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static partial class BuildOutputParser
{
    [GeneratedRegex(@":\s*(error|warning)\s+([A-Z]+\d+):\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex ErrorWarningRegex();

    [GeneratedRegex(@"Build (succeeded|FAILED)", RegexOptions.IgnoreCase)]
    private static partial Regex BuildResultRegex();

    public static BuildSummary ParseDotnetBuildOutput(string output)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            var match = ErrorWarningRegex().Match(line);
            if (match.Success)
            {
                var severity = match.Groups[1].Value.ToLowerInvariant();
                var code = match.Groups[2].Value;
                var message = match.Groups[3].Value.Trim();
                var formattedMessage = $"{code}: {message}";

                if (severity == "error")
                {
                    errors.Add(formattedMessage);
                }
                else if (severity == "warning")
                {
                    warnings.Add(formattedMessage);
                }
            }
        }

        var buildResultMatch = BuildResultRegex().Match(output);
        var success = buildResultMatch.Success && buildResultMatch.Groups[1].Value.Equals("succeeded", StringComparison.OrdinalIgnoreCase);

        return new BuildSummary(success, errors, warnings);
    }

    public static string FormatBuildSummary(BuildSummary summary, string tempFilePath, string commandName = "Build")
    {
        if (summary is { Success: true, Warnings.Count: 0 })
        {
            return $"{commandName} succeeded.";
        }

        var result = new StringBuilder();

        if (summary.Success)
        {
            result.AppendLine($"{commandName} succeeded with {summary.Warnings.Count} warning(s):");
            result.AppendLine();
            foreach (var warning in summary.Warnings.Take(5))
            {
                result.AppendLine($"  {warning}");
            }

            if (summary.Warnings.Count > 5)
            {
                result.AppendLine($"  ... and {summary.Warnings.Count - 5} more warning(s)");
            }
        }
        else
        {
            result.AppendLine($"{commandName} failed with {summary.Errors.Count} error(s):");
            result.AppendLine();
            foreach (var error in summary.Errors.Take(5))
            {
                result.AppendLine($"  {error}");
            }

            if (summary.Errors.Count > 5)
            {
                result.AppendLine($"  ... and {summary.Errors.Count - 5} more error(s)");
            }

            if (summary.Warnings.Count > 0)
            {
                result.AppendLine();
                result.AppendLine($"Also {summary.Warnings.Count} warning(s). See full output for details.");
            }
        }

        result.AppendLine();
        result.AppendLine($"Full output saved to: {tempFilePath}");

        return result.ToString();
    }

    public static string FormatTestSummary(string output)
    {
        // Extract test summary line (e.g., "Passed! - Failed: 0, Passed: 42, Skipped: 0, Total: 42")
        var lines = output.Split('\n');
        var summaryLine = lines.FirstOrDefault(l => l.Contains("Passed!") || l.Contains("Failed:"));

        if (summaryLine != null)
        {
            return $"Tests passed.\n\n{summaryLine.Trim()}";
        }

        // Look for failure indicators
        if (output.Contains("Failed!") || output.Contains("Test Run Failed"))
        {
            return "Tests failed. See full output for details.";
        }

        return "Tests completed successfully.";
    }
}

public record BuildSummary(bool Success, List<string> Errors, List<string> Warnings);
