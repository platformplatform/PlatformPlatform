using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Net.Http.Json;
using Bogus;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class CreateTestDataCommand : Command
{
    private const int ConcurrentRequests = 50;
    private const int NumberOfTenantsToCreate = 10;
    private int _countFailedRequests;
    private List<Task>? _createUserTasks;
    private HttpClient? _httpClient;
    private Stopwatch? _stopwatch;
    private ConcurrentDictionary<string, int>? _uniqueEmails;

    public CreateTestDataCommand() : base("create-test-data", "Create Tenant and Users for testing")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private HttpClient HttpClient => _httpClient ??= new HttpClient
    {
        BaseAddress = new Uri($"https://localhost:{9000}"),
        Timeout = TimeSpan.FromSeconds(5),
        DefaultRequestHeaders = { { "UserAgent", "PlatformPlatform Developer CLI" } }
    };

    private async Task Execute()
    {
        if (await IsWebSiteRunning() == false)
        {
            AnsiConsole.WriteLine("Please ensure the website is started. E.g. run `pp dev` in another terminal.");
            Environment.Exit(0);
        }

        await CreateTestData();
    }

    private async Task<bool> IsWebSiteRunning()
    {
        try
        {
            // Make API call that connects to the database
            var response = await HttpClient.GetAsync("/api/account-management/signups/is-subdomain-free?Subdomain=foo");

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task CreateTestData()
    {
        AnsiConsole.WriteLine("Creating data...");

        _stopwatch = Stopwatch.StartNew();
        _uniqueEmails = new ConcurrentDictionary<string, int>();

        try
        {
            for (var i = 1; i <= NumberOfTenantsToCreate; i++)
            {
                await CreateTenant(i * 250);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            AnsiConsole.WriteLine($"Finished creating {_uniqueEmails.Count} users in {_stopwatch.Elapsed} (average {Convert.ToInt16(_stopwatch.ElapsedMilliseconds / _uniqueEmails.Count)} ms per user). Failed requests: {_countFailedRequests}.");
        }
    }

    private async Task CreateTenant(int userCount)
    {
        try
        {
            var tenantFaker = new Faker();
            var domainName = tenantFaker.Internet.DomainName();
            var tenantId = new Uri($"https://{domainName}").Host.Split(".")[0];

            var firstName = $"{tenantFaker.Person.FirstName} {tenantFaker.Person.LastName}";
            var lastName = tenantFaker.Person.LastName;
            var email = tenantFaker.Internet.Email(firstName, lastName, domainName);
            _uniqueEmails!.TryAdd(email, 0);

            var startSignupResponse = await HttpClient.PostAsJsonAsync(
                "/api/account-management/signups/start",
                new { Subdomain = tenantId, Email = email }
            );
            await ThrowIfRequestFailed(startSignupResponse);

            var startSignupResult = (await startSignupResponse.Content.ReadFromJsonAsync<StartSignup>())!;
            var completeAccountRegistrationResponse = await HttpClient.PostAsJsonAsync(
                $"/api/account-management/signups/{startSignupResult.SignupId}/complete",
                new { oneTimePassword = "UNLOCK" } // "UNLOCK" is a magic password that always works on the dev environment
            );
            await ThrowIfRequestFailed(completeAccountRegistrationResponse);

            AnsiConsole.WriteLine($"Tenant {tenantId} created, with {email} as the owner.");

            _createUserTasks = new List<Task>();

            for (var j = 1; j < userCount; j++)
            {
                var index = j;
                _createUserTasks.Add(Task.Run(async () =>
                        {
                            var skipName = index % 40 == 0; // This will skip the name for every 40th user
                            var skipJobTitle = index % 10 == 0; // This will skip the job title for every 10th user
                            var makeOwner = index == 1; // This will make an additional Owner for each tenant
                            var makeAdmin = index is >= 2 and <= 5; // This make 4 Admins for each tenant
                            await CreateUser(tenantId, domainName, skipName, skipJobTitle, makeOwner, makeAdmin);
                        }
                    )
                );

                while (_createUserTasks!.Count(t => t.Status is TaskStatus.RanToCompletion) + ConcurrentRequests <= _createUserTasks!.Count)
                {
                    Thread.Sleep(TimeSpan.FromMicroseconds(10));
                }
            }

            await Task.WhenAll(_createUserTasks.ToArray());
        }
        catch (Exception e)
        {
            AnsiConsole.WriteLine("Failed to create tenant: " + e.Message);
            Interlocked.Increment(ref _countFailedRequests);
            throw;
        }
    }

    public async Task CreateUser(string tenantId, string domainName, bool skipName, bool skipTitle, bool makeOwner, bool makeAdmin)
    {
        try
        {
            var personFaker = new Faker();

            retry:
            var firstName = personFaker.Person.FirstName;
            var lastName = personFaker.Person.LastName;
            var jobTitle = personFaker.Name.JobTitle();
            var email = personFaker.Internet.Email(firstName, lastName, domainName);
            if (!_uniqueEmails!.TryAdd(email, 0)) goto retry; // A goto statement is more readable than a while loop :)

            var createUserResponse = await HttpClient.PostAsJsonAsync(
                "/api/account-management/users",
                new { TenantId = tenantId, Email = email, UserRole = "Member", EmailConfirmed = true }
            );
            await ThrowIfRequestFailed(createUserResponse);

            var updateUserResponse = await HttpClient.PutAsJsonAsync(
                createUserResponse.Headers.Location,
                new
                {
                    Email = email,
                    FirstName = skipName ? string.Empty : firstName,
                    LastName = skipName ? string.Empty : lastName,
                    Title = skipTitle ? string.Empty : jobTitle
                }
            );
            await ThrowIfRequestFailed(updateUserResponse);

            if (makeOwner || makeAdmin)
            {
                var changeUserRoleResponse = await HttpClient.PutAsJsonAsync(
                    $"{createUserResponse.Headers.Location}/change-user-role",
                    new { UserRole = makeOwner ? "Owner" : "Admin" }
                );
                await ThrowIfRequestFailed(changeUserRoleResponse);
            }

            var averageTimePerUser = _stopwatch!.ElapsedMilliseconds / _uniqueEmails.Count;
            AnsiConsole.WriteLine($"Completed requests: {_createUserTasks!.Count(t => t.Status is TaskStatus.RanToCompletion) + 1} users created. {_countFailedRequests} failed requests. Average time per user: {averageTimePerUser} ms.");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteLine("Failed to create user: " + e.Message);
            Interlocked.Increment(ref _countFailedRequests);
        }
    }

    private static async Task ThrowIfRequestFailed(HttpResponseMessage responseMessage)
    {
        if (!responseMessage.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(Environment.NewLine + await responseMessage.Content.ReadAsStringAsync());
        }
    }
}

public record StartSignup(string SignupId);
