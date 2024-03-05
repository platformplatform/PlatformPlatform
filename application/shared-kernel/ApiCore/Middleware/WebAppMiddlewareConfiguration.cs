namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

[UsedImplicitly]
public class WebAppMiddlewareConfiguration
{
    public WebAppMiddlewareConfiguration(
        string webAppProjectName = "WebApp",
        Dictionary<string, string>? publicEnvironmentVariables = null
    )
    {
        var publicUrl = GetEnvironmentVariableOrThrow(WebAppMiddleware.PublicUrlKey);
        var cdnUrl = GetEnvironmentVariableOrThrow(WebAppMiddleware.CdnUrlKey);
        var applicationVersion = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

        var environmentVariables = new Dictionary<string, string>
        {
            { WebAppMiddleware.PublicUrlKey, publicUrl },
            { WebAppMiddleware.CdnUrlKey, cdnUrl },
            { WebAppMiddleware.ApplicationVersion, applicationVersion }
        };

        StaticRuntimeEnvironment = publicEnvironmentVariables is null
            ? environmentVariables
            : environmentVariables.Concat(publicEnvironmentVariables).ToDictionary();

        BuildRootPath = GetWebAppDistRoot(webAppProjectName, "dist");
        HtmlTemplatePath = Path.Combine(BuildRootPath, "index.html");

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "development")
        {
            for (var i = 0; i < 10; i++)
            {
                if (File.Exists(HtmlTemplatePath)) break;
                Debug.WriteLine($"Waiting for {webAppProjectName} build to be ready...");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        if (!File.Exists(HtmlTemplatePath))
        {
            throw new FileNotFoundException("index.html does not exist.", HtmlTemplatePath);
        }
    }

    public Dictionary<string, string> StaticRuntimeEnvironment { get; }

    public string HtmlTemplatePath { get; }

    public string BuildRootPath { get; }

    private static string GetEnvironmentVariableOrThrow(string variableName)
    {
        return Environment.GetEnvironmentVariable(variableName)
               ?? throw new InvalidOperationException($"Required environment variable '{variableName}' is not set.");
    }

    private static string GetWebAppDistRoot(string webAppProjectName, string webAppDistRootName)
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        var directoryInfo = new DirectoryInfo(assemblyPath);
        while (directoryInfo is not null &&
               directoryInfo.GetDirectories(webAppProjectName).Length == 0 &&
               !Path.Exists(Path.Join(directoryInfo.FullName, webAppProjectName, webAppDistRootName))
              )
        {
            directoryInfo = directoryInfo.Parent;
        }

        return Path.Join(directoryInfo!.FullName, webAppProjectName, webAppDistRootName);
    }
}