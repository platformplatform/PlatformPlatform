using System.Net;
using System.Net.Sockets;
using AppHost;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Projects;

// Check for port conflicts before starting
CheckPortAvailability();

var builder = DistributedApplication.CreateBuilder(args);

var certificatePassword = builder.CreateSslCertificateIfNotExists();

SecretManagerHelper.GenerateAuthenticationTokenSigningKey("authentication-token-signing-key");

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

var frontendBuild = builder
    .AddNpmApp("frontend-build", "../")
    .WithEnvironment("CERTIFICATE_PASSWORD", certificatePassword);

var accountManagementDatabase = sqlServer
    .AddDatabase("account-management-database", "account-management");

var accountManagementWorkers = builder
    .AddProject<AccountManagement_Workers>("account-management-workers")
    .WithReference(accountManagementDatabase)
    .WithReference(azureStorage)
    .WaitFor(accountManagementDatabase);

var accountManagementApi = builder
    .AddProject<AccountManagement_Api>("account-management-api")
    .WithUrlConfiguration("/account-management")
    .WithReference(accountManagementDatabase)
    .WithReference(azureStorage)
    .WaitFor(accountManagementWorkers);

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

var appGateway = builder
    .AddProject<AppGateway>("app-gateway")
    .WithReference(frontendBuild)
    .WithReference(accountManagementApi)
    .WithReference(backOfficeApi)
    .WaitFor(accountManagementApi)
    .WaitFor(frontendBuild)
    .WithUrlForEndpoint("https", url => url.DisplayText = "Web App");

appGateway.WithUrl($"{appGateway.GetEndpoint("https")}/back-office", "Back Office");
appGateway.WithUrl($"{appGateway.GetEndpoint("https")}/openapi", "Open API");

await builder.Build().RunAsync();

return;

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
        Console.WriteLine("   Services already running. Please run 'pp watch --stop' first.");
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
