using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServerPassword = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
var database = builder
    .AddSqlServer("account-management-db", sqlServerPassword, 8433)
    .WithVolumeMount("sql-server-data", "/var/opt/mssql")
    .AddDatabase("account-management");

var accountManagementStorage = builder
    .AddAzureStorage("account-management-storage")
    .RunAsEmulator()
    .AddBlobs("blobs");

var accountManagementApi = builder
    .AddProject<Api>("account-management-api")
    .WithReference(database)
    .WithReference(accountManagementStorage);

builder
    .AddNpmApp("account-management-spa", "../account-management/WebApp", "dev")
    .WithReference(accountManagementApi);

builder.AddContainer("email-test-server", "mailhog/mailhog")
    .WithEndpoint(hostPort: 8025, containerPort: 8025, scheme: "http")
    .WithEndpoint(hostPort: 1025, containerPort: 1025);

builder.Build().Run();