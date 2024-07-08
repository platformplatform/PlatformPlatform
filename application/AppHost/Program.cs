using AppHost;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var certificatePassword = builder.CreateSslCertificateIfNotExists();

var sqlPassword = builder.CreateStablePassword("sql-server-password");
var sqlServer = builder.AddSqlServer("sql-server", sqlPassword, 9002)
    .WithVolume("platform-platform-sql-server-data", "/var/opt/mssql");

var azureStorage = builder
    .AddAzureStorage("azure-storage")
    .RunAsEmulator(resourceBuilder =>
        {
            resourceBuilder.WithVolume("platform-platform-azure-storage-data", "/data");
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

var accountManagementDatabase = sqlServer
    .AddDatabase("account-management-database", "account-management");

CreateBlobContainer("avatars");

var frontendBuild = builder
    .AddNpmApp("frontend-build", "../")
    .WithEnvironment("CERTIFICATE_PASSWORD", certificatePassword);

var accountManagementApi = builder
    .AddProject<AccountManagement_Api>("account-management-api")
    .WithReference(accountManagementDatabase)
    .WithReference(azureStorage);

builder
    .AddProject<AccountManagement_Workers>("account-management-workers")
    .WithReference(accountManagementDatabase)
    .WithReference(azureStorage);

var backOfficeDatabase = sqlServer
    .AddDatabase("back-office-database", "back-office");

var backOfficeApi = builder
    .AddProject<BackOffice_Api>("back-office-api")
    .WithReference(backOfficeDatabase)
    .WithReference(azureStorage);

builder
    .AddProject<BackOffice_Workers>("back-office-workers")
    .WithReference(backOfficeDatabase)
    .WithReference(azureStorage);

builder
    .AddProject<AppGateway>("app-gateway")
    .WithReference(frontendBuild)
    .WithReference(accountManagementApi)
    .WithReference(backOfficeApi);

builder.Build().Run();

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
