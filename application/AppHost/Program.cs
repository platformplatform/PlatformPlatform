using System.Diagnostics;
using AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
// var sqlPassword = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD")
//                   ?? throw new InvalidOperationException("Missing SQL_SERVER_PASSWORD environment variable.");
//
// builder.AddContainer("localhost", "mcr.microsoft.com/azure-sql-edge")
//     .WithEnvironment("ACCEPT_EULA", "Y")
//     .WithEnvironment("SA_PASSWORD", sqlPassword)
//     .WithServiceBinding(1433, "tcp", "localhost")
//     .WithVolumeMount("sql-server-data", "/var/opt/mssql", VolumeMountType.Named);

StartSqlServerUsingDockerCompose();

var accountManagementApi = builder.AddProject<PlatformPlatform_AccountManagement_Api>("account-management-api");

builder.AddBunApp("frontend", "../account-management/WebApp", "dev")
    .WithReference(accountManagementApi);

builder.Build().Run();

// Temporary workaround until staring container using builder.AddContainer() is working.
static void StartSqlServerUsingDockerCompose()
{
    try
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo { FileName = "docker", Arguments = "compose up sql-server -d" };
        process.Start();
        process.WaitForExit();
        Thread.Sleep(TimeSpan.FromSeconds(3)); // Ensure SQL Server is ready
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
        throw;
    }
}