using Microsoft.Data.Sqlite;

namespace FinanceTracker.Tests.Helpers;

// Создать in-memory SQLite БД с тестовой схемой (таблица Transactions).
public static class InMemoryDbHelper
{
    public const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS Transactions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Date TEXT NOT NULL,
            Amount REAL NOT NULL,
            Category TEXT NOT NULL,
            Description TEXT
        );
        """;

    public static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = CreateTableSql;
        cmd.ExecuteNonQuery();
        return connection;
    }

    public static string GetConnectionString() => "Data Source=:memory:";
}
