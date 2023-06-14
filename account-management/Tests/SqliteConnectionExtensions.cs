using System.Reflection;
using Microsoft.Data.Sqlite;

namespace PlatformPlatform.AccountManagement.Tests;

public static class SqliteConnectionExtensions
{
    public static long ExecuteScalar(this SqliteConnection connection, string sql, object? parameters = null)
    {
        using var command = new SqliteCommand(sql, connection);

        foreach (var property in parameters?.GetType().GetProperties() ?? Array.Empty<PropertyInfo>())
        {
            command.Parameters.AddWithValue(property.Name, property.GetValue(parameters));
        }

        return (long) command.ExecuteScalar()!;
    }

    public static bool RowExists(this SqliteConnection connection, string tableName, string id)
    {
        return connection.ExecuteScalar($"SELECT COUNT(*) FROM {tableName} WHERE Id = @id", new {id}) == 1;
    }
}