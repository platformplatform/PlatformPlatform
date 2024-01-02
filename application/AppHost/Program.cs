using System.Diagnostics;
using AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

StartSqlServerUsingDockerCompose();

var accountManagementApi = builder.AddProject<PlatformPlatform_AccountManagement_Api>("account-management-api");

builder.AddBunApp("frontend", "../account-management/WebApp", "dev")
    .WithReference(accountManagementApi);

builder.Build().Run();

return;

// Temporary workaround until staring SQL Server using Aspire is fixed: https://github.com/dotnet/aspire/issues/1023
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