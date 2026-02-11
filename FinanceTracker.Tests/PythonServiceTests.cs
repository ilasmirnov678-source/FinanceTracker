using System.IO;
using System.Text.Json;
using FinanceTracker.Models.Analytics;
using FinanceTracker.Services;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.Tests;

public class PythonServiceTests
{
    [Fact]
    public void DeserializeResult_ValidJson_ReturnsAnalytics()
    {
        string json = """{"by_category":[{"name":"Food","sum":150.0},{"name":"Transport","sum":200.0}],"by_month":[{"month":"2025-02","sum":350.0}],"total":350.0}""";
        var result = PythonService.DeserializeResult(json);

        result.Should().NotBeNull();
        result.ByCategory.Should().HaveCount(2);
        result.ByCategory.Should().Contain(c => c.Name == "Food" && c.Sum == 150.0);
        result.ByCategory.Should().Contain(c => c.Name == "Transport" && c.Sum == 200.0);
        result.ByMonth.Should().HaveCount(1);
        result.ByMonth[0].Month.Should().Be("2025-02");
        result.ByMonth[0].Sum.Should().Be(350.0);
        result.Total.Should().Be(350.0);
    }

    [Fact]
    public void DeserializeResult_EmptyArrays_ReturnsEmptyResult()
    {
        string json = """{"by_category":[],"by_month":[],"total":0}""";
        var result = PythonService.DeserializeResult(json);

        result.Should().NotBeNull();
        result.ByCategory.Should().BeEmpty();
        result.ByMonth.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public void DeserializeResult_EmptyString_Throws()
    {
        var act = () => PythonService.DeserializeResult("");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*пустой вывод*");
    }

    [Fact]
    public void DeserializeResult_WhitespaceOnly_Throws()
    {
        var act = () => PythonService.DeserializeResult("   ");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*пустой вывод*");
    }

    [Fact]
    public void DeserializeResult_InvalidJson_Throws()
    {
        var act = () => PythonService.DeserializeResult("{ invalid }");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*разбора JSON*")
            .WithInnerException<JsonException>();
    }

    [Fact]
    public void ResolvePythonPath_NoVenv_ReturnsPython()
    {
        string tempDir = Path.GetTempPath();
        string result = PythonService.ResolvePythonPath(tempDir);

        result.Should().Be("python");
    }

    [Fact]
    public void ResolvePythonPath_VenvExists_ReturnsVenvPath()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "FinanceTrackerTest_" + Guid.NewGuid().ToString("N"));
        string venvPython = Path.Combine(tempDir, "PythonApp", "venv", "Scripts", "python.exe");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(venvPython)!);
            File.WriteAllText(venvPython, "");
            string result = PythonService.ResolvePythonPath(tempDir);
            result.Should().Be(venvPython);
        }
        finally
        {
            try { File.Delete(venvPython); } catch { }
            try { Directory.Delete(Path.Combine(tempDir, "PythonApp"), true); } catch { }
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void BuildArguments_ContainsDbFromTo()
    {
        var service = new PythonService();
        string args = service.BuildArguments(@"C:\data\finance.db", new DateTime(2025, 2, 1), new DateTime(2025, 3, 15));

        args.Should().Contain("--db");
        args.Should().Contain(@"C:\data\finance.db");
        args.Should().Contain("--from");
        args.Should().Contain("2025-02-01");
        args.Should().Contain("--to");
        args.Should().Contain("2025-03-15");
    }

    [Fact]
    public async Task GenerateReportAsync_ScriptNotFound_ThrowsFileNotFoundException()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "FinanceTrackerTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new PythonService(tempDir);
            string dbPath = Path.Combine(tempDir, "test.db");
            File.WriteAllText(dbPath, "");

            var act = () => service.GenerateReportAsync(dbPath, new DateTime(2025, 1, 1), new DateTime(2025, 2, 1));

            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*Скрипт аналитики не найден*");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
