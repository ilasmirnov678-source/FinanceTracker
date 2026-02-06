using System.IO;

namespace FinanceTracker.Services;

// Сервис для инициализации и управления подключением к базе данных SQLite.
public class DatabaseService
{
    private string _dbPath;

    // Путь к файлу базы данных.
    public string DbPath
    {
        get => _dbPath;
        set => _dbPath = value ?? throw new ArgumentNullException(nameof(value));
    }

    // Строка подключения к базе данных.
    public string ConnectionString => "Data Source=" + _dbPath;

    // Конструктор с опциональным путём к БД.
    public DatabaseService(string? dbPath = null)
    {
        _dbPath = dbPath ?? "Database/finance.db";
    }

    // Прочитать SQL-скрипт из файла schema.sql.
    private string ReadSchemaSql()
    {
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "schema.sql");
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException($"Файл schema.sql не найден по пути: {schemaPath}", schemaPath);
        return File.ReadAllText(schemaPath);
    }
}
