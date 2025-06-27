namespace PlatformPlatform.DeveloperCli.Utilities;

public static class CommandLineArgumentsPreprocessor
{
    public const string EscapedAtSymbolMarker = "ESCAPED_AT_SYMBOL";

    /// <summary>
    ///     Preprocesses command-line arguments to handle special cases like positional arguments with @ symbols.
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

            // Handle positional arguments that start with @ (for e2e search terms)
            if (arg.StartsWith("@"))
            {
                // Replace @ with a special marker that won't trigger response file handling
                var escapedValue = EscapedAtSymbolMarker + arg.Substring(1);
                result.Add(escapedValue);
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
