using System.Net;
using System.Net.Sockets;
using AppHost;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Projects;

// Check for port conflicts before starting
CheckPortAvailability();

var builder = DistributedApplication.CreateBuilder(args);

var certificatePassword = await builder.CreateSslCertificateIfNotExists();

SecretManagerHelper.GenerateAuthenticationTokenSigningKey("authentication-token-signing-key");

var (googleOAuthConfigured, googleOAuthClientId, googleOAuthClientSecret) = ConfigureGoogleOAuthParameters();

var (stripeConfigured, stripePublishableKey, stripeApiKey, stripeWebhookSecret) = ConfigureStripeParameters();
var stripeFullyConfigured = stripeConfigured && builder.Configuration["Parameters:stripe-webhook-secret"] is not null and not "not-configured";

var sqlPassword = builder.CreateStablePassword("sql-server-password");
var sqlServer = builder.AddSqlServer("sql-server", sqlPassword, 9002)
    .WithDataVolume("platform-platform-sql-server-data")
    .WithLifetime(ContainerLifetime.Persistent);

var azureStorage = builder
    .AddAzureStorage("azure-storage")
    .RunAsEmulator(resourceBuilder =>
        {
            resourceBuilder.WithDataVolume("platform-platform-azure-storage-data");
            resourceBuilder.WithBlobPort(10000);
            resourceBuilder.WithLifetime(ContainerLifetime.Persistent);
        }
    )
    .WithAnnotation(new ContainerImageAnnotation
        {
            Registry = "mcr.microsoft.com",
            Image = "azure-storage/azurite",
            Tag = "latest"
        }
    )
    .AddBlobs("blobs");

builder
    .AddContainer("mail-server", "axllent/mailpit")
    .WithHttpEndpoint(9003, 8025)
    .WithEndpoint(9004, 1025)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithUrlForEndpoint("http", u => u.DisplayText = "Read mail here");

CreateBlobContainer("avatars");
CreateBlobContainer("logos");

var frontendBuild = builder
    .AddJavaScriptApp("frontend-build", "../")
    .WithEnvironment("CERTIFICATE_PASSWORD", certificatePassword);

var accountDatabase = sqlServer
    .AddDatabase("account-database", "account");

var accountWorkers = builder
    .AddProject<Account_Workers>("account-workers")
    .WithReference(accountDatabase)
    .WithReference(azureStorage)
    .WaitFor(accountDatabase);

var accountApi = builder
    .AddProject<Account_Api>("account-api")
    .WithUrlConfiguration("/account")
    .WithReference(accountDatabase)
    .WithReference(azureStorage)
    .WithEnvironment("OAuth__Google__ClientId", googleOAuthClientId)
    .WithEnvironment("OAuth__Google__ClientSecret", googleOAuthClientSecret)
    .WithEnvironment("OAuth__AllowMockProvider", "true")
    .WithEnvironment("Stripe__SubscriptionEnabled", stripeFullyConfigured ? "true" : "false")
    .WithEnvironment("Stripe__ApiKey", stripeApiKey)
    .WithEnvironment("Stripe__WebhookSecret", stripeWebhookSecret)
    .WithEnvironment("Stripe__PublishableKey", stripePublishableKey)
    .WithEnvironment("Stripe__AllowMockProvider", "true")
    .WaitFor(accountWorkers);

var backOfficeDatabase = sqlServer
    .AddDatabase("back-office-database", "back-office");

var backOfficeWorkers = builder
    .AddProject<BackOffice_Workers>("back-office-workers")
    .WithReference(backOfficeDatabase)
    .WithReference(azureStorage)
    .WaitFor(backOfficeDatabase);

var backOfficeApi = builder
    .AddProject<BackOffice_Api>("back-office-api")
    .WithUrlConfiguration("/back-office")
    .WithReference(backOfficeDatabase)
    .WithReference(azureStorage)
    .WaitFor(backOfficeWorkers);

var mainDatabase = sqlServer
    .AddDatabase("main-database", "main");

var mainWorkers = builder
    .AddProject<Main_Workers>("main-workers")
    .WithReference(mainDatabase)
    .WithReference(azureStorage)
    .WaitFor(mainDatabase);

var mainApi = builder
    .AddProject<Main_Api>("main-api")
    .WithUrlConfiguration("")
    .WithReference(mainDatabase)
    .WithReference(azureStorage)
    .WithEnvironment("PUBLIC_GOOGLE_OAUTH_ENABLED", googleOAuthConfigured ? "true" : "false")
    .WithEnvironment("PUBLIC_SUBSCRIPTION_ENABLED", stripeFullyConfigured ? "true" : "false")
    .WaitFor(mainWorkers);

var appGateway = builder
    .AddProject<AppGateway>("app-gateway")
    .WithReference(frontendBuild)
    .WithReference(accountApi)
    .WithReference(backOfficeApi)
    .WithReference(mainApi)
    .WaitFor(accountApi)
    .WaitFor(frontendBuild)
    .WithUrlForEndpoint("https", url => url.DisplayText = "Web App");

appGateway.WithUrl($"{appGateway.GetEndpoint("https")}/back-office", "Back Office");
appGateway.WithUrl($"{appGateway.GetEndpoint("https")}/openapi", "Open API");

AddStripeCliContainer();

await builder.Build().RunAsync();

return;

void AddStripeCliContainer()
{
    if (stripeConfigured)
    {
        builder
            .AddContainer("stripe-cli", "stripe/stripe-cli:latest")
            .WithArgs("listen", "--forward-to", "https://host.docker.internal:9000/api/account/subscriptions/stripe-webhook", "--skip-verify")
            .WithEnvironment("STRIPE_API_KEY", stripeApiKey)
            .WithLifetime(ContainerLifetime.Persistent);
    }
}

(bool Configured, IResourceBuilder<ParameterResource> ClientId, IResourceBuilder<ParameterResource> ClientSecret) ConfigureGoogleOAuthParameters()
{
    _ = builder.AddParameter("google-oauth-enabled")
        .WithDescription("""
                         **Google OAuth** -- Enables "Sign in with Google" for login and signup using OpenID Connect with PKCE.

                         **Important**: Set up OAuth credentials in the [Google Cloud Console](https://console.cloud.google.com/apis/credentials) and configure them according to the guide in README.md **before** enabling this.

                         - Enter `true` to enable Google OAuth, or `false` to skip. This can be changed later.
                         - After enabling, **restart Aspire** to be prompted for the Client ID and Client Secret.

                         See **README.md** for full setup instructions.
                         """, true
        );

    var configured = builder.Configuration["Parameters:google-oauth-enabled"] == "true";

    if (configured)
    {
        var clientId = builder.AddParameter("google-oauth-client-id", true)
            .WithDescription("""
                             Google OAuth Client ID from the [Google Cloud Console](https://console.cloud.google.com/apis/credentials). The format is `<id>.apps.googleusercontent.com`.

                             **After entering this and the Client Secret, restart Aspire** to apply the configuration.

                             See **README.md** for full setup instructions.
                             """, true
            );
        var clientSecret = builder.AddParameter("google-oauth-client-secret", true)
            .WithDescription("""
                             Google OAuth Client Secret from the [Google Cloud Console](https://console.cloud.google.com/apis/credentials).

                             **After entering this and the Client ID, restart Aspire** to apply the configuration.

                             See **README.md** for full setup instructions.
                             """, true
            );

        return (configured, clientId, clientSecret);
    }

    return (
        configured,
        builder.CreateResourceBuilder(new ParameterResource("google-oauth-client-id", _ => "not-configured", true)),
        builder.CreateResourceBuilder(new ParameterResource("google-oauth-client-secret", _ => "not-configured", true))
    );
}

(bool Configured, IResourceBuilder<ParameterResource> PublishableKey, IResourceBuilder<ParameterResource> ApiKey, IResourceBuilder<ParameterResource> WebhookSecret) ConfigureStripeParameters()
{
    _ = builder.AddParameter("stripe-enabled")
        .WithDescription("""
                         **Stripe Integration** -- Enables embedded checkout, prorated plan upgrades, automatic tax management, localized invoices, billing history with refunds, and more.

                         **Important**: Set up a [Stripe sandbox environment](https://dashboard.stripe.com) and configure it according to the guide in README.md **before** enabling this.

                         - Enter `true` to enable Stripe, or `false` to skip. This can be changed later.
                         - Setup requires **2 restarts** after enabling: first for API keys, then for the webhook secret from the stripe-cli container.

                         See **README.md** for full setup instructions.
                         """, true
        );

    var configured = builder.Configuration["Parameters:stripe-enabled"] == "true";

    if (configured)
    {
        var publishableKey = builder.AddParameter("stripe-publishable-key", true)
            .WithDescription("""
                             Stripe Publishable Key from the [Stripe Dashboard](https://dashboard.stripe.com/apikeys). Starts with `pk_test_` or `pk_live_`.

                             **After entering this and the Secret Key, restart Aspire.** The stripe-cli container will start and generate a webhook secret, which you will enter on the next restart.

                             See **README.md** for full setup instructions.
                             """, true);
        var apiKey = builder.AddParameter("stripe-api-key", true)
            .WithDescription("""
                             Stripe Secret Key from the [Stripe Dashboard](https://dashboard.stripe.com/apikeys). Starts with `sk_test_` or `sk_live_`.

                             **After entering this and the Publishable Key, restart Aspire.** The stripe-cli container will start and generate a webhook secret, which you will enter on the next restart.

                             See **README.md** for full setup instructions.
                             """, true);

        var apiKeyConfigured = builder.Configuration["Parameters:stripe-api-key"] is not null;
        var webhookSecret = apiKeyConfigured
            ? builder.AddParameter("stripe-webhook-secret", true)
                .WithDescription("Webhook signing secret. Find it in the [Stripe Dashboard Workbench](https://dashboard.stripe.com/test/workbench/webhooks) or in the stripe-cli container logs after the previous restart. Starts with `whsec_`.", true)
            : builder.CreateResourceBuilder(new ParameterResource("stripe-webhook-secret", _ => "not-configured", true));

        return (configured, publishableKey, apiKey, webhookSecret);
    }

    return (
        configured,
        builder.CreateResourceBuilder(new ParameterResource("stripe-publishable-key", _ => "not-configured", true)),
        builder.CreateResourceBuilder(new ParameterResource("stripe-api-key", _ => "not-configured", true)),
        builder.CreateResourceBuilder(new ParameterResource("stripe-webhook-secret", _ => "not-configured", true))
    );
}

void CreateBlobContainer(string containerName)
{
    var connectionString = builder.Configuration.GetConnectionString("blob-storage");

    new Task(() =>
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            containerClient.CreateIfNotExists();
        }
    ).Start();
}

void CheckPortAvailability()
{
    Thread.Sleep(500); // Allow time for previous process to fully release ports

    var ports = new[] { (9098, "Resource Service"), (9097, "Dashboard"), (9001, "Aspire") };
    var blocked = ports.Where(p => !IsPortAvailable(p.Item1)).ToList();

    if (blocked.Any())
    {
        Console.WriteLine($"⚠️  Port conflicts: {string.Join(", ", blocked.Select(b => $"{b.Item1} ({b.Item2})"))}");
        Console.WriteLine("   Services already running. Stop them first using 'watch --stop' command or the watch MCP tool with stop flag.");
        Environment.Exit(1);
    }

    bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
