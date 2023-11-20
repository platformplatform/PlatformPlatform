using AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var accountManagementApi = builder.AddProject<PlatformPlatform_AccountManagement_Api>("account-management-api");

builder.AddBunApp("frontend", "../account-management/WebApp", "dev")
    .WithReference(accountManagementApi);

builder.Build().Run();