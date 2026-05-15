using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using SharedKernel.Configuration;
using SharedKernel.ExecutionContext;
using SharedKernel.FeatureFlags;

namespace SharedKernel.Telemetry;

public class ApplicationInsightsTelemetryInitializer : ITelemetryInitializer
{
    private static readonly AsyncLocal<IExecutionContext> ExecutionContext = new();
    private static readonly AsyncLocal<string?> PublicUrlOverride = new();

    public void Initialize(ITelemetry telemetry)
    {
        // Apply deployment metadata to every telemetry item, including PageViews/Exceptions/Metrics
        // forwarded from the SPA via /api/track (where IExecutionContext is not always set).
        if (SharedInfrastructureConfiguration.ServiceVersion is not null)
        {
            telemetry.Context.Component.Version = SharedInfrastructureConfiguration.ServiceVersion;
        }

        AddCustomProperty(telemetry, "deployment.commit_hash", SharedInfrastructureConfiguration.DeploymentCommitHash);
        AddCustomProperty(telemetry, "deployment.github_action_id", SharedInfrastructureConfiguration.DeploymentGithubActionId);

        var executionContext = ExecutionContext.Value;

        if (executionContext is null)
        {
            return;
        }

        // Override the request URL with the public host when the AI SDK path emits a RequestTelemetry.
        // OpenTelemetry instrumentation is the primary source of request telemetry (see
        // PublicHostTelemetryEnricher) -- this branch is belt-and-suspenders for any AI-SDK-only path.
        var publicUrl = PublicUrlOverride.Value;
        if (publicUrl is not null && telemetry is RequestTelemetry requestTelemetry &&
            Uri.TryCreate(publicUrl, UriKind.Absolute, out var absoluteUri))
        {
            requestTelemetry.Url = absoluteUri;
        }

        telemetry.Context.Location.Ip = executionContext.ClientIpAddress.ToString();

        if (executionContext.TenantId is not null)
        {
            telemetry.Context.User.AccountId = executionContext.TenantId.ToString();
        }

        if (executionContext.UserInfo.Id is not null)
        {
            telemetry.Context.User.Id = executionContext.UserInfo.Id;
        }

        if (executionContext.UserInfo.IsAuthenticated)
        {
            telemetry.Context.User.AuthenticatedUserId = executionContext.UserInfo.Id!;
        }

        // Also track TenantId and UserId as custom properties, to be consistent with OpenTelemetry where build-in properties cannot be tracked
        // Set custom properties, ensure any changes here are also added to OpenTelemetryEnricher
        AddCustomProperty(telemetry, "tenant.id", executionContext.TenantId?.Value);
        AddCustomProperty(telemetry, "tenant.subscription_plan", executionContext.UserInfo.SubscriptionPlan);
        AddCustomProperty(telemetry, "user.id", executionContext.UserInfo.Id);
        AddCustomProperty(telemetry, "user.is_authenticated", executionContext.UserInfo.IsAuthenticated);
        AddCustomProperty(telemetry, "user.locale", executionContext.UserInfo.Locale);
        AddCustomProperty(telemetry, "user.zoom_level", executionContext.UserInfo.ZoomLevel);
        AddCustomProperty(telemetry, "user.theme", executionContext.UserInfo.Theme);
        AddCustomProperty(telemetry, "user.role", executionContext.UserInfo.Role);
        AddCustomProperty(telemetry, "user.session_id", executionContext.UserInfo.SessionId?.Value);

        foreach (var (name, value) in FeatureFlagTelemetryProperties.GetEnabledFeatureFlagTags(executionContext.UserInfo.FeatureFlags))
        {
            AddCustomProperty(telemetry, name, value);
        }
    }

    public static void SetContext(IExecutionContext executionContext)
    {
        ExecutionContext.Value = executionContext;
    }

    public static void SetPublicUrl(string? publicUrl)
    {
        PublicUrlOverride.Value = publicUrl;
    }

    private static void AddCustomProperty(ITelemetry telemetry, string name, object? value)
    {
        var stringValue = value?.ToString();
        if (stringValue is null) return;
        telemetry.Context.GlobalProperties[name] = stringValue;
    }
}
