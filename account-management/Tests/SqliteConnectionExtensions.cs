using Microsoft.Data.Sqlite;

namespace PlatformPlatform.AccountManagement.Tests;

public static class SqliteConnectionExtensions
{
    public static long ExecuteScalar(this SqliteConnection connection, string sql, params object?[] parameters)
    {
        using var command = new SqliteCommand(sql, connection);

        foreach (var parameter in parameters)
        {
            foreach (var property in parameter?.GetType().GetProperties() ?? Array.Empty<PropertyInfo>())
            {
                command.Parameters.AddWithValue(property.Name, property.GetValue(parameter));
            }
        }

        return (long) command.ExecuteScalar()!;
    }

    public static bool RowExists(this SqliteConnection connection, string tableName, string id)
    {
        return connection.ExecuteScalar($"SELECT COUNT(*) FROM {tableName} WHERE Id = @id", new {id}) == 1;
    }
}