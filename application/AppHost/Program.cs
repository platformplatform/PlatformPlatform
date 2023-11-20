using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<PlatformPlatform_AccountManagement_Api>("account-management-api");

builder.Build().Run();