using AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD")
                  ?? throw new InvalidOperationException("Missing SQL_SERVER_PASSWORD environment variable.");

builder.AddContainer("localhost", "mcr.microsoft.com/azure-sql-edge")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("SA_PASSWORD", sqlPassword)
    .WithServiceBinding(1433, "tcp", "localhost")
    .WithVolumeMount("sql-server-data", "/var/opt/mssql", VolumeMountType.Named);

var accountManagementApi = builder.AddProject<PlatformPlatform_AccountManagement_Api>("account-management-api");

builder.AddBunApp("frontend", "../account-management/WebApp", "dev")
    .WithReference(accountManagementApi);

builder.Build().Run();