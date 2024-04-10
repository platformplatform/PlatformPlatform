using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServerPassword = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
var sqlServer = builder
    .AddSqlServer("sql-server", sqlServerPassword, 9002)
    .WithVolumeMount("sql-server-data", "/var/opt/mssql");

var azureStorage = builder
    .AddAzureStorage("azure-storage")
    .RunAsEmulator(resourceBuilder =>
    {
        resourceBuilder.WithVolumeMount("azure-storage-data", "/data");
        resourceBuilder.UseBlobPort(10000);
    })
    .AddBlobs("blobs");

builder
    .AddContainer("mail-server", "mailhog/mailhog")
    .WithEndpoint(hostPort: 9003, containerPort: 8025, scheme: "http")
    .WithEndpoint(hostPort: 9004, containerPort: 1025);

var accountManagementDatabase = sqlServer
    .AddDatabase("account-management-database", "account-management");

CreateBlobContainer("avatars");

var accountManagementApi = builder
    .AddProject<Api>("account-management-api")
    .WithReference(accountManagementDatabase)
    .WithReference(azureStorage);

var accountManagementSpa = builder
    .AddNpmApp("account-management-spa", "../account-management/WebApp", "dev")
    .WithReference(accountManagementApi);

builder
    .AddProject<AppGateway>("app-gateway")
    .WithReference(accountManagementApi)
    .WithReference(accountManagementSpa);

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
    }).Start();
}