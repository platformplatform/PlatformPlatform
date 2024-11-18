using System.Diagnostics;
using AppHost;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Projects;

// Detect if Aspire ports from the previous run are released. See https://github.com/dotnet/aspire/issues/6704
EnsureDeveloperControlPaneIsNotRunning();

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
    .WithEndpoint(9004, 1025);

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
    .WithReference(backOfficeDatabase)
    .WithReference(azureStorage)
    .WaitFor(backOfficeWorkers);

builder
    .AddProject<AppGateway>("app-gateway")
    .WithReference(frontendBuild)
    .WithReference(accountManagementApi)
    .WithReference(backOfficeApi)
    .WaitFor(accountManagementApi)
    .WaitFor(frontendBuild);

await builder.Build().RunAsync();

return;

void EnsureDeveloperControlPaneIsNotRunning()
{
    const string processName = "dcpctrl"; // The Aspire Developer Control Pane process name

    var process = Process.GetProcesses()
        .SingleOrDefault(p => p.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase));

    if (process == null) return;

    Console.WriteLine($"Shutting down developer control pane from previous run. Process: {process.ProcessName} (ID: {process.Id})");

    Thread.Sleep(TimeSpan.FromSeconds(5)); // Allow Docker containers to shut down to avoid orphaned containers

    try
    {
        process.Kill();
        Console.WriteLine($"Process {process.Id} killed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
    }
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
