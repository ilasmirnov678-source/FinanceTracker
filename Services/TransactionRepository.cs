using Microsoft.Data.Sqlite;
using FinanceTracker.Models;

namespace FinanceTracker.Services;

// Репозиторий для работы с таблицей Transactions. Не создаёт БД и таблицы.
public class TransactionRepository
{
    private readonly string _connectionString;

    // Путь к файлу БД или строка подключения; при первом запуске БД создаётся сервисом.
    public TransactionRepository(string connectionStringOrPath)
    {
        if (string.IsNullOrWhiteSpace(connectionStringOrPath))
            throw new ArgumentException("Connection string or path required.", nameof(connectionStringOrPath));

        _connectionString = connectionStringOrPath.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            ? connectionStringOrPath
            : "Data Source=" + connectionStringOrPath;
    }

    // Получить все транзакции.
    public List<Transaction> GetAll()
    {
        var result = new List<Transaction>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Date, Amount, Category, Description FROM Transactions ORDER BY Date DESC";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(MapRowToTransaction(reader));
        return result;
    }

    // Получить транзакции за указанный период дат.
    public List<Transaction> GetByDateRange(DateTime from, DateTime to)
    {
        var result = new List<Transaction>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Date, Amount, Category, Description FROM Transactions WHERE Date >= @From AND Date < @To ORDER BY Date DESC";
        command.Parameters.AddWithValue("@From", from.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@To", to.Date.AddDays(1).ToString("yyyy-MM-dd"));
        using var reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(MapRowToTransaction(reader));
        return result;
    }

    // Добавить новую транзакцию.
    public void Add(Transaction transaction)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Transactions (Date, Amount, Category, Description) VALUES (@Date, @Amount, @Category, @Description); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@Date", transaction.Date.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@Amount", transaction.Amount);
        command.Parameters.AddWithValue("@Category", transaction.Category);
        command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(transaction.Description) ? string.Empty : transaction.Description);
        var newId = (long)command.ExecuteScalar();
        transaction.Id = (int)newId;
    }

    // Обновить существующую транзакцию.
    public void Update(Transaction transaction)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Transactions SET Date = @Date, Amount = @Amount, Category = @Category, Description = @Description WHERE Id = @Id";
        command.Parameters.AddWithValue("@Date", transaction.Date.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@Amount", transaction.Amount);
        command.Parameters.AddWithValue("@Category", transaction.Category);
        command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(transaction.Description) ? string.Empty : transaction.Description);
        command.Parameters.AddWithValue("@Id", transaction.Id);
        command.ExecuteNonQuery();
    }

    // Удалить транзакцию по идентификатору.
    public void Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Transactions WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    // Маппинг строки результата в объект Transaction.
    private static Transaction MapRowToTransaction(SqliteDataReader reader)
    {
        return new Transaction
        {
            Id = reader.GetInt32(0),
            Date = DateTime.Parse(reader.GetString(1)),
            Amount = reader.GetDecimal(2),
            Category = reader.GetString(3),
            Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
        };
    }
}
