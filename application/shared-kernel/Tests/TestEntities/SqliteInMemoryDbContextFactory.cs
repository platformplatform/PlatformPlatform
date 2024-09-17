using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class SqliteInMemoryDbContextFactory<T> : IDisposable where T : DbContext
{
    private readonly IExecutionContext _executionContext;
    private readonly SqliteConnection _sqliteConnection;

    public SqliteInMemoryDbContextFactory(IExecutionContext executionContext)
    {
        _executionContext = executionContext;
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();
    }

    public void Dispose()
    {
        _sqliteConnection.Close();
    }

    public T CreateContext()
    {
        var options = CreateOptions();

        var context = (T)Activator.CreateInstance(typeof(T), options, _executionContext)!;
        context.Database.EnsureCreated();

        return context;
    }

    private DbContextOptions<T> CreateOptions()
    {
        return new DbContextOptionsBuilder<T>().UseSqlite(_sqliteConnection).Options;
    }
}
