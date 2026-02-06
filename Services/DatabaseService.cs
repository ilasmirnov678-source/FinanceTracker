using System.IO;
using Microsoft.Data.Sqlite;

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
        var path = dbPath ?? "Database/finance.db";
        // Преобразовать относительный путь в абсолютный, если нужно.
        _dbPath = Path.IsPathRooted(path) ? path : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        
        // Создать директорию для БД, если её нет.
        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        
        // Инициализировать БД, если файл не существует.
        if (!File.Exists(_dbPath))
            InitializeDatabase();
    }

    // Прочитать SQL-скрипт из файла schema.sql.
    private string ReadSchemaSql()
    {
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "schema.sql");
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException($"Файл schema.sql не найден по пути: {schemaPath}", schemaPath);
        return File.ReadAllText(schemaPath);
    }

    // Инициализировать базу данных: создать файл и выполнить schema.sql.
    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            
            var sql = ReadSchemaSql();
            // Разделить SQL-команды по точке с запятой и выполнить каждую.
            var commands = sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            foreach (var commandText in commands)
            {
                // Пропустить пустые строки и комментарии.
                var trimmed = commandText.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--"))
                    continue;
                
                using var command = connection.CreateCommand();
                command.CommandText = trimmed;
                command.ExecuteNonQuery();
            }
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException($"Ошибка при инициализации базы данных: {ex.Message}", ex);
        }
    }
}
