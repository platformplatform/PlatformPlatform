using Main.Database;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.Email;
using SharedKernel.Telemetry;
using SharedKernel.Tests.Telemetry;

namespace Main.Tests;

// Seeds the schema and fixtures once into an in-memory template database, then fills each test's
// connection with a fast binary copy via SqliteConnection.BackupDatabase instead of running
// EnsureCreated and reseeding for every test. Because every test database is a binary copy of the
// template, the shared DatabaseSeeder's entity references (ids, emails) match the rows in every test's
// connection, even though the seeder generates fresh random ids on each run.
internal static class SeededDatabaseTemplate
{
    // Fixed-name shared-cache in-memory database holding the seeded template. The keep-alive connection
    // below keeps it alive for the process lifetime; every test copies its schema and data from here.
    private const string TemplateConnectionString = "Data Source=MainTestTemplate;Mode=Memory;Cache=Shared";

    // Guards one-time seeding and serializes the binary copies: the single template connection is the
    // copy source, so only one BackupDatabase can read it at a time.
    private static readonly Lock SyncLock = new();

    private static SqliteConnection? _template;

    private static DatabaseSeeder? _seeder;

    // Seeds the template on first use and returns the shared seeder whose entity references match the
    // rows copied into every test's connection.
    public static DatabaseSeeder EnsureSeeded()
    {
        lock (SyncLock)
        {
            if (_seeder is null)
            {
                Seed();
            }

            return _seeder!;
        }
    }

    // Copies the seeded schema and data into a test's connection. BackupDatabase is a binary page copy,
    // far cheaper than recreating the schema and reseeding per test.
    public static void RestoreInto(SqliteConnection destination)
    {
        lock (SyncLock)
        {
            _template!.BackupDatabase(destination);
        }
    }

    private static void Seed()
    {
        _template = new SqliteConnection(TemplateConnectionString);
        _template.Open();
        ApplyPragmas(_template);

        // Seed through the real dependency injection graph so Entity Framework interceptors behave like
        // production. The substitutes stand in for dependencies the seeding path never exercises.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransient<DatabaseSeeder>();
        services.AddDbContext<MainDbContext>(options => options.UseSqlite(_template).UseSnakeCaseNamingConvention());
        services.AddMainServices();
        services.AddScoped<ITelemetryEventsCollector>(_ => new TelemetryEventsCollectorSpy(new TelemetryEventsCollector()));
        services.AddScoped<IEmailClient>(_ => Substitute.For<IEmailClient>());
        services.AddSingleton(new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = Substitute.For<ITelemetryChannel>() }));
        services.AddScoped<IExecutionContext, HttpExecutionContext>();

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<MainDbContext>().Database.EnsureCreated();
        _seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    }

    private static void ApplyPragmas(SqliteConnection connection)
    {
        // Configure SQLite to behave more like PostgreSQL while the template schema is created.
        using var command = connection.CreateCommand();

        // Enable foreign key constraints (PostgreSQL has this by default)
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        // Enable recursive triggers (PostgreSQL supports nested triggers)
        command.CommandText = "PRAGMA recursive_triggers = ON;";
        command.ExecuteNonQuery();

        // Enforce CHECK constraints (PostgreSQL enforces these by default)
        command.CommandText = "PRAGMA ignore_check_constraints = OFF;";
        command.ExecuteNonQuery();

        // Use more strict query parsing
        command.CommandText = "PRAGMA trusted_schema = OFF;";
        command.ExecuteNonQuery();
    }
}
