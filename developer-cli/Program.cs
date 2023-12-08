using PlatformPlatform.DeveloperCli;

ChangeDetection.EnsureCliIsCompiledWithLatestChanges(args);

Console.WriteLine($"Run command with arguments: {string.Join(", ", args)}");
