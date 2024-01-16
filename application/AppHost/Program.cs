using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServerPassword = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
var database = builder.AddSqlServerContainer("Default", sqlServerPassword, 1433)
    .WithVolumeMount("sql-server-data", "/var/opt/mssql", VolumeMountType.Named)
    .AddDatabase("account-management");

var accountManagementApi = builder.AddProject<PlatformPlatform_AccountManagement_Api>("account-management-api")
    .WithReference(database);

builder.AddNpmApp("frontend", "../account-management/WebApp", "dev")
    .WithReference(accountManagementApi);

builder.Build().Run();