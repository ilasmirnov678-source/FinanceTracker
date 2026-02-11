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

    // Временная директория с PythonApp/analyzer.py (произвольное содержимое) и пустым файлом БД.
    private static (string tempDir, string dbPath) CreateTempDirWithAnalyzer(string analyzerScriptContent)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "FinanceTrackerTest_" + Guid.NewGuid().ToString("N"));
        string pythonAppDir = Path.Combine(tempDir, "PythonApp");
        string scriptPath = Path.Combine(pythonAppDir, "analyzer.py");
        Directory.CreateDirectory(pythonAppDir);
        File.WriteAllText(scriptPath, analyzerScriptContent);
        string dbPath = Path.Combine(tempDir, "dummy.db");
        File.WriteAllBytes(dbPath, Array.Empty<byte>());
        return (tempDir, dbPath);
    }

    private static void TryDeleteTempDir(string tempDir)
    {
        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
        catch (IOException) { }
    }

    [Fact]
    [Trait("Category", "Slow")]
    public async Task GenerateReportAsync_ThrowsInvalidOperationException_WhenScriptExceedsTimeout()
    {
        string script = """
            import time
            import sys
            sys.stderr.write("sleeping...")
            sys.stderr.flush()
            time.sleep(25)
            print('{"by_category":[],"by_month":[],"total":0}')
            """;
        var (tempDir, dbPath) = CreateTempDirWithAnalyzer(script);
        try
        {
            var service = new PythonService(tempDir);

            var act = () => service.GenerateReportAsync(dbPath, new DateTime(2025, 1, 1), new DateTime(2025, 2, 1));

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Таймаут выполнения анализатора (15 с)*")
                .WithMessage("*sleeping...*");
        }
        finally
        {
            TryDeleteTempDir(tempDir);
        }
    }

    [Fact]
    [Trait("Category", "Slow")]
    public async Task GenerateReportAsync_ThrowsOperationCanceledException_WhenUserCancels()
    {
        string script = """
            import time
            import sys
            time.sleep(25)
            print('{"by_category":[],"by_month":[],"total":0}')
            """;
        var (tempDir, dbPath) = CreateTempDirWithAnalyzer(script);
        try
        {
            var service = new PythonService(tempDir);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1));

            var act = () => service.GenerateReportAsync(dbPath, new DateTime(2025, 1, 1), new DateTime(2025, 2, 1), cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            TryDeleteTempDir(tempDir);
        }
    }

    [Fact]
    public async Task GenerateReportAsync_ThrowsInvalidOperationExceptionWithStderr_WhenScriptExitsNonZero()
    {
        string script = """
            import sys
            sys.stderr.write("custom error")
            sys.exit(1)
            """;
        var (tempDir, dbPath) = CreateTempDirWithAnalyzer(script);
        try
        {
            var service = new PythonService(tempDir);

            var act = () => service.GenerateReportAsync(dbPath, new DateTime(2025, 1, 1), new DateTime(2025, 2, 1));

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*кодом 1*")
                .WithMessage("*custom error*");
        }
        finally
        {
            TryDeleteTempDir(tempDir);
        }
    }
}
