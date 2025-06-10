namespace PlatformPlatform.DeveloperCli.Utilities;

public static class CommandLineArgumentsPreprocessor
{
    public const string EscapedAtSymbolMarker = "ESCAPED_AT_SYMBOL";

    /// <summary>
    ///     Preprocesses command-line arguments to handle special cases like --grep with @ symbols.
    ///     This works around System.CommandLine's response file handling which treats @ as a response file indicator.
    /// </summary>
    /// <param name="args">The original command-line arguments</param>
    /// <returns>Preprocessed command-line arguments</returns>
    public static string[] PreprocessArguments(string[] args)
    {
        var result = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            // Check if this is --grep followed by a value
            if ((arg == "--grep" || arg == "-g") && i + 1 < args.Length)
            {
                // Add the --grep argument
                result.Add(arg);

                var grepValue = args[i + 1];

                // Handle @ symbol at the beginning (response file issue)
                if (grepValue.StartsWith("@"))
                {
                    // Replace @ with a special marker that won't trigger response file handling
                    var escapedValue = EscapedAtSymbolMarker + grepValue.Substring(1);
                    result.Add(escapedValue);
                }
                else
                {
                    // For other grep patterns, ensure they're properly quoted
                    // This helps with patterns containing spaces or special characters
                    result.Add(grepValue);
                }

                // Skip the next argument as we've already processed it
                i++;
            }
            else
            {
                // Add the argument as-is
                result.Add(arg);
            }
        }

        return result.ToArray();
    }
}
