using System.IO;
using FinanceTracker.Models.Analytics;
using FinanceTracker.Services;
using FinanceTracker.Tests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FinanceTracker.Tests;

// Интеграционный тест: реальный запуск analyzer.py, временная файловая БД. Требует Python и pandas.
public class PythonServiceIntegrationTests
{
    private static string CreateTempDbWithData()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), "FinanceTrackerTest_" + Guid.NewGuid().ToString("N") + ".db");
        using (var conn = new SqliteConnection("Data Source=" + dbPath))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = InMemoryDbHelper.CreateTableSql;
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO Transactions (Date, Amount, Category, Description) VALUES ('2025-02-01', 100.0, 'Food', ''), ('2025-02-15', 50.0, 'Food', ''), ('2025-02-20', 200.0, 'Transport', '')";
            cmd.ExecuteNonQuery();
        }
        return dbPath;
    }

    [Fact]
    public async Task GenerateReportAsync_ReturnsAnalytics_WhenDbHasData()
    {
        string dbPath = CreateTempDbWithData();
        try
        {
            var service = new PythonService();
            var result = await service.GenerateReportAsync(dbPath, new DateTime(2025, 2, 1), new DateTime(2025, 3, 1));

            result.Should().NotBeNull();
            result.ByCategory.Should().HaveCount(2);
            result.ByCategory.Should().Contain(c => c.Name == "Food" && Math.Abs(c.Sum - 150.0) < 0.01);
            result.ByCategory.Should().Contain(c => c.Name == "Transport" && Math.Abs(c.Sum - 200.0) < 0.01);
            result.ByMonth.Should().HaveCount(1);
            result.ByMonth[0].Month.Should().Be("2025-02");
            result.ByMonth[0].Sum.Should().BeApproximately(350.0, 0.01);
            result.Total.Should().BeApproximately(350.0, 0.01);
        }
        finally
        {
            TryDeleteTempDb(dbPath);
        }
    }

    [Fact]
    public async Task GenerateReportAsync_ReturnsEmptyResult_WhenPeriodHasNoData()
    {
        string dbPath = CreateTempDbWithData();
        try
        {
            var service = new PythonService();
            var result = await service.GenerateReportAsync(dbPath, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            result.Should().NotBeNull();
            result.ByCategory.Should().BeEmpty();
            result.ByMonth.Should().BeEmpty();
            result.Total.Should().Be(0);
        }
        finally
        {
            TryDeleteTempDb(dbPath);
        }
    }

    private static void TryDeleteTempDb(string dbPath)
    {
        try
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
        catch (IOException)
        {
            // Файл может быть ещё занят (антивирус, задержка освобождения).
        }
    }
}
