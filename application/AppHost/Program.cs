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
        resourceBuilder.WithVolumeMount("azure-storage-data", "/var/opt/azurestorage");
        resourceBuilder.UseBlobPort(10000);
    })
    .AddBlobs("blobs");

builder
    .AddContainer("mail-server", "mailhog/mailhog")
    .WithEndpoint(hostPort: 9003, containerPort: 8025, scheme: "http")
    .WithEndpoint(hostPort: 9004, containerPort: 1025);

var accountManagementDatabase = sqlServer
    .AddDatabase("account-management-database", "account-management");

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