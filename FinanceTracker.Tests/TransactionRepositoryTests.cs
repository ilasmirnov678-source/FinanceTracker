using FinanceTracker.Models;
using FinanceTracker.Services;
using FinanceTracker.Tests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FinanceTracker.Tests;

public class TransactionRepositoryTests
{
    private static TransactionRepository CreateRepositoryWithSchema()
    {
        var dbName = "memdb" + Guid.NewGuid().ToString("N");
        var connectionString = "Data Source=file:" + dbName + "?mode=memory&cache=shared";
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = InMemoryDbHelper.CreateTableSql;
            cmd.ExecuteNonQuery();
        }
        return new TransactionRepository(connectionString);
    }

    [Fact]
    public void Constructor_Throws_WhenConnectionStringEmpty()
    {
        var act = () => new TransactionRepository("");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("connectionStringOrPath");
    }

    [Fact]
    public void Constructor_Throws_WhenConnectionStringNull()
    {
        var act = () => new TransactionRepository(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_Throws_WhenConnectionStringWhiteSpace()
    {
        var act = () => new TransactionRepository("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_SetsId_AndPersistsTransaction()
    {
        var repo = CreateRepositoryWithSchema();
        var transaction = new Transaction
        {
            Date = new DateTime(2025, 2, 1),
            Amount = 100.50m,
            Category = "Продукты",
            Description = "Тест"
        };

        repo.Add(transaction);

        transaction.Id.Should().BeGreaterThan(0);

        var all = repo.GetAll();
        all.Should().HaveCount(1);
        all[0].Id.Should().Be(transaction.Id);
        all[0].Amount.Should().Be(100.50m);
        all[0].Category.Should().Be("Продукты");
        all[0].Description.Should().Be("Тест");
    }

    [Fact]
    public void GetAll_ReturnsAllTransactions_OrderedByDateDesc()
    {
        var repo = CreateRepositoryWithSchema();
        var t1 = new Transaction { Date = new DateTime(2025, 2, 1), Amount = 100, Category = "A", Description = "" };
        var t2 = new Transaction { Date = new DateTime(2025, 2, 3), Amount = 200, Category = "B", Description = "" };

        repo.Add(t1);
        repo.Add(t2);

        var all = repo.GetAll();

        all.Should().HaveCount(2);
        all[0].Date.Should().Be(new DateTime(2025, 2, 3));
        all[1].Date.Should().Be(new DateTime(2025, 2, 1));
    }

    [Fact]
    public void GetByDateRange_ReturnsOnlyTransactionsInRange()
    {
        var repo = CreateRepositoryWithSchema();
        var t1 = new Transaction { Date = new DateTime(2025, 2, 1), Amount = 100, Category = "A", Description = "" };
        var t2 = new Transaction { Date = new DateTime(2025, 2, 3), Amount = 200, Category = "B", Description = "" };
        var t3 = new Transaction { Date = new DateTime(2025, 2, 5), Amount = 300, Category = "C", Description = "" };

        repo.Add(t1);
        repo.Add(t2);
        repo.Add(t3);

        var result = repo.GetByDateRange(new DateTime(2025, 2, 2), new DateTime(2025, 2, 4));

        result.Should().HaveCount(1);
        result[0].Date.Should().Be(new DateTime(2025, 2, 3));
    }

    [Fact]
    public void Update_ModifiesExistingTransaction()
    {
        var repo = CreateRepositoryWithSchema();
        var transaction = new Transaction
        {
            Date = new DateTime(2025, 2, 1),
            Amount = 100,
            Category = "Продукты",
            Description = "Исходное"
        };
        repo.Add(transaction);

        transaction.Amount = 250;
        transaction.Category = "Транспорт";
        transaction.Description = "Обновлено";
        repo.Update(transaction);

        var all = repo.GetAll();
        all.Should().HaveCount(1);
        all[0].Amount.Should().Be(250);
        all[0].Category.Should().Be("Транспорт");
        all[0].Description.Should().Be("Обновлено");
    }

    [Fact]
    public void Delete_RemovesTransaction()
    {
        var repo = CreateRepositoryWithSchema();
        var transaction = new Transaction
        {
            Date = new DateTime(2025, 2, 1),
            Amount = 100,
            Category = "A",
            Description = ""
        };
        repo.Add(transaction);
        var id = transaction.Id;

        repo.Delete(id);

        repo.GetAll().Should().BeEmpty();
    }
}
