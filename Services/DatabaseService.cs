using System.IO;
using FinanceTracker;
using Microsoft.Data.Sqlite;

namespace FinanceTracker.Services;

// Сервис для инициализации и управления подключением к базе данных SQLite.
public class DatabaseService
{
    private readonly string _dbPath;

    // Путь к файлу базы данных.
    public string DbPath => _dbPath;

    // Строка подключения к базе данных.
    public string ConnectionString => "Data Source=" + _dbPath;

    // Конструктор с опциональным путём к БД.
    public DatabaseService(string? dbPath = null)
    {
        var path = dbPath ?? AppConstants.DefaultDbRelativePath;
        _dbPath = Path.IsPathRooted(path) 
            ? path 
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        
        EnsureDirectoryExists();
        EnsureDatabaseInitialized();
    }

    // Создать директорию для БД, если её нет.
    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    // Инициализировать БД, если нужно.
    private void EnsureDatabaseInitialized()
    {
        if (!File.Exists(_dbPath) || !TableExists())
            InitializeDatabase();
    }

    // Проверить существование таблицы Transactions.
    private bool TableExists()
    {
        if (!File.Exists(_dbPath))
            return false;

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='Transactions'";
        return command.ExecuteScalar() != null;
    }

    // Прочитать SQL-скрипт из файла schema.sql.
    private string ReadSchemaSql()
    {
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.DatabaseFolder, AppConstants.SchemaFileName);
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException($"Файл {AppConstants.SchemaFileName} не найден по пути: {schemaPath}", schemaPath);
        return File.ReadAllText(schemaPath);
    }

    // Инициализировать базу данных: создать файл и выполнить schema.sql.
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        
        var sql = ReadSchemaSql();
        var commands = ParseSqlCommands(sql);
        
        foreach (var command in commands)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
        }
    }

    // Распарсить SQL-скрипт: удалить комментарии и разделить на команды.
    private IEnumerable<string> ParseSqlCommands(string sql)
    {
        var lines = sql
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("--"));
        var fullSql = string.Join("\n", lines);
        return fullSql
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(cmd => cmd.Trim())
            .Where(cmd => !string.IsNullOrWhiteSpace(cmd));
    }
}
