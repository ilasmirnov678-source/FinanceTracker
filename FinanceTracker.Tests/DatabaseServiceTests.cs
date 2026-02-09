using FinanceTracker.Models;
using FinanceTracker.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FinanceTracker.Tests;

public class DatabaseServiceTests : IDisposable
{
    private readonly List<string> _tempPaths = [];

    public void Dispose()
    {
        foreach (var path in _tempPaths)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { /* ignore cleanup errors */ }
        }
    }

    private string CreateTempDbPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".db");
        _tempPaths.Add(path);
        return path;
    }

    [Fact]
    public void Constructor_CreatesDatabaseFile_WhenNotExists()
    {
        var dbPath = CreateTempDbPath();
        File.Exists(dbPath).Should().BeFalse();

        var service = new DatabaseService(dbPath);

        File.Exists(dbPath).Should().BeTrue();
        service.DbPath.Should().Be(dbPath);
    }

    [Fact]
    public void Constructor_CreatesTransactionsTable()
    {
        var dbPath = CreateTempDbPath();
        var service = new DatabaseService(dbPath);

        using var connection = new SqliteConnection(service.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='Transactions'";
        var result = command.ExecuteScalar();

        result.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsCustomDbPath()
    {
        var dbPath = CreateTempDbPath();
        var service = new DatabaseService(dbPath);

        service.DbPath.Should().Be(dbPath);
        service.ConnectionString.Should().Contain(dbPath);
    }

    [Fact]
    public void Constructor_InitializedDatabase_CanBeUsedByRepository()
    {
        var dbPath = CreateTempDbPath();
        var service = new DatabaseService(dbPath);
        var repository = new TransactionRepository(service.ConnectionString);

        var transaction = new Transaction
        {
            Date = DateTime.Today,
            Amount = 100,
            Category = "Test",
            Description = ""
        };
        repository.Add(transaction);

        var all = repository.GetAll();
        all.Should().HaveCount(1);
        all[0].Amount.Should().Be(100);
    }
}
