using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public sealed class SqliteInMemoryDbContextFactory<T> : IDisposable where T : DbContext
{
    private readonly SqliteConnection _sqliteConnection;

    public SqliteInMemoryDbContextFactory()
    {
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

        var context = (T) Activator.CreateInstance(typeof(T), options)!;
        context.Database.EnsureCreated();

        return context;
    }

    private DbContextOptions<T> CreateOptions()
    {
        return new DbContextOptionsBuilder<T>().UseSqlite(_sqliteConnection).Options;
    }
}