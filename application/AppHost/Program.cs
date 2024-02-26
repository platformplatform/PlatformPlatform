using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServerPassword = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
var database = builder.AddSqlServerContainer("account-management-db", sqlServerPassword, 8433)
    .WithVolumeMount("sql-server-data", "/var/opt/mssql", VolumeMountType.Named)
    .AddDatabase("account-management");

var accountManagementApi = builder.AddProject<Api>("account-management-api")
    .WithReference(database);

builder.AddNpmApp("account-management-spa", "../account-management/WebApp", "dev")
    .WithReference(accountManagementApi);

builder.Build().Run();