using Microsoft.Data.Sqlite;

namespace PlatformPlatform.SharedKernel.Tests.Persistence;

public static class SqliteConnectionExtensions
{
    [Obsolete("Use ExecuteScalar<long> instead")]
    public static long ExecuteScalar(this SqliteConnection connection, string sql, params object?[] parameters)
    {
        return connection.ExecuteScalar<long>(sql, parameters);
    }

    public static T ExecuteScalar<T>(this SqliteConnection connection, string sql, params object?[] parameters)
    {
        using var command = new SqliteCommand(sql, connection);

        foreach (var parameter in parameters)
        {
            foreach (var property in parameter?.GetType().GetProperties() ?? [])
            {
                command.Parameters.AddWithValue(property.Name, property.GetValue(parameter));
            }
        }

        var result = command.ExecuteScalar();
        return result is DBNull ? default! : (T)result!;
    }

    public static bool RowExists(this SqliteConnection connection, string tableName, string id)
    {
        object?[] parameters = [new { id }];
        return connection.ExecuteScalar<long>($"SELECT COUNT(*) FROM {tableName} WHERE Id = @id", parameters) == 1;
    }

    public static bool RowExists(this SqliteConnection connection, string tableName, long id)
    {
        object?[] parameters = [new { id }];
        return connection.ExecuteScalar<long>($"SELECT COUNT(*) FROM {tableName} WHERE Id = @id", parameters) == 1;
    }

    public static void Insert(this SqliteConnection connection, string tableName, (string, object?)[] columns)
    {
        var columnsNames = string.Join(", ", columns.Select(c => c.Item1));
        var columnsValues = string.Join(", ", columns.Select(c => "@" + c.Item1));
        var insertCommandText = $"INSERT INTO {tableName} ({columnsNames}) VALUES ({columnsValues})";
        using var command = new SqliteCommand(insertCommandText, connection);
        foreach (var column in columns)
        {
            var valueType = column.Item2?.GetType();

            var sqliteType = valueType switch
            {
                not null when valueType == typeof(int) => SqliteType.Integer,
                not null when valueType == typeof(long) => SqliteType.Integer,
                not null when valueType == typeof(bool) => SqliteType.Integer,
                not null when valueType == typeof(double) => SqliteType.Real,
                not null when valueType == typeof(float) => SqliteType.Real,
                not null when valueType == typeof(decimal) => SqliteType.Real,
                not null when valueType == typeof(byte[]) => SqliteType.Blob,
                not null when valueType == typeof(string) => SqliteType.Text,
                not null when valueType == typeof(DateTime) => SqliteType.Text, // SQLite stores dates as text
                not null when valueType == typeof(Guid) => SqliteType.Text, // Store GUIDs as text
                null => SqliteType.Text, // Handle null values by setting SqliteType to Text
                _ => SqliteType.Text // Default to Text if the type is unknown
            };
            var parameter = new SqliteParameter($"@{column.Item1}", sqliteType) { Value = column.Item2 ?? DBNull.Value };
            command.Parameters.Add(parameter);
        }

        command.ExecuteNonQuery();
    }

    public static void Update(this SqliteConnection connection, string tableName, string idColumnName, object idValue, (string, object?)[] columns)
    {
        var setClause = string.Join(", ", columns.Select(c => $"{c.Item1} = @{c.Item1}"));
        var updateCommandText = $"UPDATE {tableName} SET {setClause} WHERE {idColumnName} = @{idColumnName}";

        using var command = new SqliteCommand(updateCommandText, connection);

        // Add ID parameter
        var idValueType = idValue.GetType();
        var idSqliteType = idValueType switch
        {
            not null when idValueType == typeof(int) => SqliteType.Integer,
            not null when idValueType == typeof(long) => SqliteType.Integer,
            _ => SqliteType.Text
        };
        var idParameter = new SqliteParameter($"@{idColumnName}", idSqliteType) { Value = idValue };
        command.Parameters.Add(idParameter);

        // Add column parameters
        foreach (var column in columns)
        {
            var valueType = column.Item2?.GetType();

            var sqliteType = valueType switch
            {
                not null when valueType == typeof(int) => SqliteType.Integer,
                not null when valueType == typeof(long) => SqliteType.Integer,
                not null when valueType == typeof(bool) => SqliteType.Integer,
                not null when valueType == typeof(double) => SqliteType.Real,
                not null when valueType == typeof(float) => SqliteType.Real,
                not null when valueType == typeof(decimal) => SqliteType.Real,
                not null when valueType == typeof(byte[]) => SqliteType.Blob,
                not null when valueType == typeof(string) => SqliteType.Text,
                not null when valueType == typeof(DateTime) => SqliteType.Text, // SQLite stores dates as text
                not null when valueType == typeof(Guid) => SqliteType.Text, // Store GUIDs as text
                null => SqliteType.Text, // Handle null values by setting SqliteType to Text
                _ => SqliteType.Text // Default to Text if the type is unknown
            };
            var parameter = new SqliteParameter($"@{column.Item1}", sqliteType) { Value = column.Item2 ?? DBNull.Value };
            command.Parameters.Add(parameter);
        }

        command.ExecuteNonQuery();
    }

    public static void Delete(this SqliteConnection connection, string tableName, string id)
    {
        using var command = new SqliteCommand($"DELETE FROM {tableName} WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }
}
