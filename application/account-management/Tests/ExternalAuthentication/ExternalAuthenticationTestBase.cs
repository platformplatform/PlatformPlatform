using System.Net;
using System.Text.Json;
using System.Web;
using Bogus;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.SinglePageApp;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Tests.Telemetry;

namespace PlatformPlatform.AccountManagement.Tests.ExternalAuthentication;

public abstract class ExternalAuthenticationTestBase : IDisposable
{
    protected readonly Faker Faker = new();
    protected readonly TimeProvider TimeProvider;
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    protected TelemetryEventsCollectorSpy TelemetryEventsCollectorSpy;

    protected ExternalAuthenticationTestBase()
    {
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey, "https://localhost:9000");
        Environment.SetEnvironmentVariable(SinglePageAppConfiguration.CdnUrlKey, "https://localhost:9000/account-management");
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost;LiveEndpoint=https://localhost"
        );
        Environment.SetEnvironmentVariable("BypassAntiforgeryValidation", "true");

        TimeProvider = TimeProvider.System;

        Connection = new SqliteConnection($"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        Connection.Open();

        using (var command = Connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = ON;";
            command.ExecuteNonQuery();
            command.CommandText = "PRAGMA recursive_triggers = ON;";
            command.ExecuteNonQuery();
            command.CommandText = "PRAGMA ignore_check_constraints = OFF;";
            command.ExecuteNonQuery();
            command.CommandText = "PRAGMA trusted_schema = OFF;";
            command.ExecuteNonQuery();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransient<DatabaseSeeder>();
        services.AddDbContext<AccountManagementDbContext>(options => { options.UseSqlite(Connection); });
        services.AddAccountManagementServices();

        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
        services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);

        var emailClient = Substitute.For<IEmailClient>();
        services.AddScoped<IEmailClient>(_ => emailClient);

        var telemetryChannel = Substitute.For<ITelemetryChannel>();
        services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = telemetryChannel }));

        services.AddScoped<IExecutionContext, HttpExecutionContext>();

        using var serviceProvider = services.BuildServiceProvider();
        using var serviceScope = serviceProvider.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<AccountManagementDbContext>().Database.EnsureCreated();
        DatabaseSeeder = serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => { logging.AddFilter(_ => false); });

                builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["OAuth:AllowMockProvider"] = "true"
                            }
                        );
                    }
                );

                builder.ConfigureTestServices(testServices =>
                    {
                        testServices.Remove(testServices.Single(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AccountManagementDbContext>)));
                        testServices.AddDbContext<AccountManagementDbContext>(options => { options.UseSqlite(Connection); });

                        TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
                        testServices.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);

                        testServices.Remove(testServices.Single(d => d.ServiceType == typeof(IEmailClient)));
                        testServices.AddTransient<IEmailClient>(_ => emailClient);

                        testServices.AddScoped<IExecutionContext, HttpExecutionContext>();
                    }
                );
            }
        );

        NoRedirectHttpClient = _webApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        NoRedirectHttpClient.DefaultRequestHeaders.Add("User-Agent", "TestBrowser/1.0");
        NoRedirectHttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US");
        NoRedirectHttpClient.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
    }

    protected SqliteConnection Connection { get; }

    protected DatabaseSeeder DatabaseSeeder { get; }

    protected HttpClient NoRedirectHttpClient { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async Task<(string CallbackUrl, string[] Cookies)> StartLoginFlow(string? returnPath = null, string? locale = null, TenantId? preferredTenantId = null)
    {
        var url = BuildStartUrl("login", returnPath, locale, preferredTenantId);
        var response = await NoRedirectHttpClient.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return (response.Headers.Location!.ToString(), ExtractSetCookieHeaders(response));
    }

    protected async Task<(string CallbackUrl, string[] Cookies)> StartSignupFlow(string? returnPath = null, string? locale = null)
    {
        var url = BuildStartUrl("signup", returnPath, locale);
        var response = await NoRedirectHttpClient.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return (response.Headers.Location!.ToString(), ExtractSetCookieHeaders(response));
    }

    protected async Task<HttpResponseMessage> CallCallback(string callbackUrl, IEnumerable<string> cookies, string flowType = "login")
    {
        var uri = ToAbsoluteUri(callbackUrl);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var requestUrl = $"{uri.AbsolutePath}?code={Uri.EscapeDataString(queryParams["code"]!)}&state={Uri.EscapeDataString(queryParams["state"]!)}";
        var request = CreateRequestWithCookies(HttpMethod.Get, requestUrl, cookies);

        return await NoRedirectHttpClient.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> CallCallbackWithError(string callbackUrl, IEnumerable<string> cookies, string error, string? errorDescription = null)
    {
        var uri = ToAbsoluteUri(callbackUrl);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var requestUrl = $"{uri.AbsolutePath}?state={Uri.EscapeDataString(queryParams["state"]!)}&error={Uri.EscapeDataString(error)}";
        if (errorDescription is not null) requestUrl += $"&error_description={Uri.EscapeDataString(errorDescription)}";

        var request = CreateRequestWithCookies(HttpMethod.Get, requestUrl, cookies);
        return await NoRedirectHttpClient.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> CallCallbackWithoutCode(string callbackUrl, IEnumerable<string> cookies)
    {
        var uri = ToAbsoluteUri(callbackUrl);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var requestUrl = $"{uri.AbsolutePath}?state={Uri.EscapeDataString(queryParams["state"]!)}";
        var request = CreateRequestWithCookies(HttpMethod.Get, requestUrl, cookies);
        return await NoRedirectHttpClient.SendAsync(request);
    }

    protected string GetExternalLoginIdFromResponse(HttpResponseMessage startResponse)
    {
        var location = startResponse.Headers.Location!.ToString();
        return GetExternalLoginIdFromUrl(location);
    }

    protected string GetExternalLoginIdFromUrl(string url)
    {
        var uri = ToAbsoluteUri(url);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        var state = queryParams["state"];

        using var scope = _webApplicationFactory.Services.CreateScope();
        var externalAuthService = scope.ServiceProvider.GetRequiredService<ExternalAuthenticationService>();
        var externalLoginId = externalAuthService.GetExternalLoginIdFromState(state);
        return externalLoginId!.ToString();
    }

    protected void ExpireExternalLogin(string externalLoginId)
    {
        var expiredTime = TimeProvider.GetUtcNow().AddSeconds(-(ExternalLogin.ValidForSeconds + 1));
        Connection.Update("ExternalLogins", "Id", externalLoginId, [("CreatedAt", expiredTime)]);
    }

    protected void TamperWithNonce(string externalLoginId)
    {
        Connection.Update("ExternalLogins", "Id", externalLoginId, [("Nonce", "tampered-nonce-value")]);
    }

    protected UserId InsertUserWithExternalIdentity(string email, ExternalProviderType providerType, string providerUserId)
    {
        var userId = UserId.NewId();
        var identities = JsonSerializer.Serialize(new[] { new { Provider = providerType.ToString(), ProviderUserId = providerUserId } });
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", userId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", email.ToLower()),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US"),
                ("ExternalIdentities", identities)
            ]
        );
        return userId;
    }

    [UsedImplicitly]
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        Connection.Close();
        _webApplicationFactory.Dispose();
    }

    private static string BuildStartUrl(string flowType, string? returnPath, string? locale, TenantId? preferredTenantId = null)
    {
        var url = $"/api/account-management/authentication/Google/{flowType}/start";
        var queryParams = new List<string>();
        if (returnPath is not null) queryParams.Add($"returnPath={Uri.EscapeDataString(returnPath)}");
        if (locale is not null) queryParams.Add($"locale={locale}");
        if (preferredTenantId is not null) queryParams.Add($"preferredTenantId={preferredTenantId}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);
        return url;
    }

    private static Uri ToAbsoluteUri(string url)
    {
        var uri = new Uri(url, UriKind.RelativeOrAbsolute);
        return uri.IsAbsoluteUri ? uri : new Uri(new Uri("https://localhost:9000"), url);
    }

    private static string[] ExtractSetCookieHeaders(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies.ToArray() : [];
    }

    protected async Task<HttpResponseMessage> CallCallbackWithTamperedState(string callbackUrl, IEnumerable<string> cookies)
    {
        var uri = ToAbsoluteUri(callbackUrl);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var requestUrl = $"{uri.AbsolutePath}?code={Uri.EscapeDataString(queryParams["code"]!)}&state=garbage-tampered-data";
        var request = CreateRequestWithCookies(HttpMethod.Get, requestUrl, cookies);
        return await NoRedirectHttpClient.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> CallCallbackWithCrossedFlows(string callbackUrl, IEnumerable<string> crossedCookies)
    {
        var uri = ToAbsoluteUri(callbackUrl);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var requestUrl = $"{uri.AbsolutePath}?code={Uri.EscapeDataString(queryParams["code"]!)}&state={Uri.EscapeDataString(queryParams["state"]!)}";
        var request = CreateRequestWithCookies(HttpMethod.Get, requestUrl, crossedCookies);
        return await NoRedirectHttpClient.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> CallCallbackWithTamperedCookie(string callbackUrl, string tamperedCookieValue)
    {
        var uri = ToAbsoluteUri(callbackUrl);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var requestUrl = $"{uri.AbsolutePath}?code={Uri.EscapeDataString(queryParams["code"]!)}&state={Uri.EscapeDataString(queryParams["state"]!)}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.TryAddWithoutValidation("Cookie", $"__Host-external-login={tamperedCookieValue}");
        request.Headers.TryAddWithoutValidation("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        return await NoRedirectHttpClient.SendAsync(request);
    }

    private static HttpRequestMessage CreateRequestWithCookies(HttpMethod method, string requestUrl, IEnumerable<string> cookies)
    {
        var request = new HttpRequestMessage(method, requestUrl);
        foreach (var cookie in cookies)
        {
            var cookieParts = cookie.Split(';')[0];
            request.Headers.TryAddWithoutValidation("Cookie", cookieParts);
        }

        request.Headers.TryAddWithoutValidation("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        return request;
    }
}
