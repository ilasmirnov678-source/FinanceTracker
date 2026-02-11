using FinanceTracker.Models;
using FinanceTracker.Models.Analytics;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinanceTracker.Tests;

public class MainViewModelTests
{
    private static (Mock<IPythonService> python, string dbPath) CreatePythonServiceAndPath()
    {
        var mock = new Mock<IPythonService>();
        mock.Setup(p => p.GenerateReportAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Models.Analytics.AnalyticsResult());
        return (mock, "test.db");
    }

    [Fact]
    public void Refresh_LoadsTransactionsFromRepository()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var transactions = new List<Transaction>
        {
            new() { Id = 1, Date = new DateTime(2025, 2, 15), Amount = 100, Category = "A", Description = "" },
            new() { Id = 2, Date = new DateTime(2025, 2, 10), Amount = 200, Category = "B", Description = "" }
        };
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(transactions);
        var (mockPython, dbPath) = CreatePythonServiceAndPath();

        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);

        vm.Transactions.Should().HaveCount(2);
        vm.Transactions[0].Id.Should().Be(1);
        vm.Transactions[0].Amount.Should().Be(100);
        vm.Transactions[1].Id.Should().Be(2);
        mockRepo.Verify(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public void Refresh_ClearsPreviousTransactions()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var firstCall = new List<Transaction>
        {
            new() { Id = 1, Date = DateTime.Today, Amount = 100, Category = "A", Description = "" }
        };
        var secondCall = new List<Transaction>
        {
            new() { Id = 2, Date = DateTime.Today, Amount = 200, Category = "B", Description = "" }
        };
        var callCount = 0;
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(() => ++callCount == 1 ? firstCall : secondCall);
        var (mockPython, dbPath) = CreatePythonServiceAndPath();

        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        vm.Transactions.Should().HaveCount(1);
        vm.Transactions[0].Id.Should().Be(1);

        vm.RefreshCommand.Execute(null);

        vm.Transactions.Should().HaveCount(1);
        vm.Transactions[0].Id.Should().Be(2);
    }

    [Fact]
    public void CanEditOrDelete_ReturnsFalse_WhenNoSelection()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var (mockPython, dbPath) = CreatePythonServiceAndPath();

        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        vm.SelectedTransaction = null;

        vm.EditTransactionCommand.CanExecute(null).Should().BeFalse();
        vm.DeleteTransactionCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CanEditOrDelete_ReturnsTrue_WhenSelected()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var transaction = new Transaction
        {
            Id = 1,
            Date = DateTime.Today,
            Amount = 100,
            Category = "Test",
            Description = ""
        };
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction> { transaction });
        var (mockPython, dbPath) = CreatePythonServiceAndPath();

        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        vm.SelectedTransaction = vm.Transactions[0];

        vm.EditTransactionCommand.CanExecute(null).Should().BeTrue();
        vm.DeleteTransactionCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void StartDateFilterChanged_TriggersRefresh()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var (mockPython, dbPath) = CreatePythonServiceAndPath();

        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        mockRepo.Invocations.Clear();

        vm.StartDateFilter = new DateTime(2025, 1, 1);

        mockRepo.Verify(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.AtLeastOnce);
    }

    [Fact]
    public void EndDateFilterChanged_TriggersRefresh()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var (mockPython, dbPath) = CreatePythonServiceAndPath();

        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        mockRepo.Invocations.Clear();

        vm.EndDateFilter = new DateTime(2025, 2, 28);

        mockRepo.Verify(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateReportCommand_PassesReportStartDateAndReportEndDate_ToService()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var mockPython = new Mock<IPythonService>();
        DateTime? capturedFrom = null;
        DateTime? capturedTo = null;
        mockPython.Setup(p => p.GenerateReportAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, DateTime, CancellationToken>((_, from, to, _) =>
            {
                capturedFrom = from;
                capturedTo = to;
            })
            .ReturnsAsync(new AnalyticsResult());
        string dbPath = "test.db";
        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        vm.ReportStartDate = new DateTime(2025, 1, 10);
        vm.ReportEndDate = new DateTime(2025, 2, 20);

        vm.GenerateReportCommand.Execute(null);
        await Task.Delay(500);

        capturedFrom.Should().Be(new DateTime(2025, 1, 10));
        capturedTo.Should().Be(new DateTime(2025, 2, 20));
        mockPython.Verify(p => p.GenerateReportAsync(dbPath, vm.ReportStartDate, vm.ReportEndDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReportCommand_WhenServiceThrows_SetsReportErrorAndClearsIsReportGenerating()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var mockPython = new Mock<IPythonService>();
        const string errorMessage = "Таймаут выполнения анализатора (15 с).";
        mockPython.Setup(p => p.GenerateReportAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));
        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, "test.db");

        vm.GenerateReportCommand.Execute(null);
        await Task.Delay(500);

        vm.ReportError.Should().Be(errorMessage);
        vm.IsReportGenerating.Should().BeFalse();
    }

    [Fact]
    public void ReportChartType_ByCategory_ShowsOnlyCategoryChart()
    {
        var (mockPython, dbPath) = CreatePythonServiceAndPath();
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        vm.SelectedReportChartTypeItem = vm.ReportChartTypeItems.First(x => x.Value == ReportChartType.ByCategory);

        vm.IsCategoryChartVisible.Should().BeTrue();
        vm.IsMonthChartVisible.Should().BeFalse();
    }

    [Fact]
    public void ReportChartType_ByMonth_ShowsOnlyMonthChart()
    {
        var (mockPython, dbPath) = CreatePythonServiceAndPath();
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        vm.SelectedReportChartTypeItem = vm.ReportChartTypeItems.First(x => x.Value == ReportChartType.ByMonth);

        vm.IsCategoryChartVisible.Should().BeFalse();
        vm.IsMonthChartVisible.Should().BeTrue();
    }

    [Fact]
    public void ReportChartType_Both_ShowsBothCharts()
    {
        var (mockPython, dbPath) = CreatePythonServiceAndPath();
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);
        vm.SelectedReportChartTypeItem = vm.ReportChartTypeItems.First(x => x.Value == ReportChartType.Both);

        vm.IsCategoryChartVisible.Should().BeTrue();
        vm.IsMonthChartVisible.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateReportCommand_WhenResultEmpty_SetsReportEmptyMessage()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var mockPython = new Mock<IPythonService>();
        mockPython.Setup(p => p.GenerateReportAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AnalyticsResult { ByCategory = [], ByMonth = [], Total = 0 });
        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, "test.db");

        vm.GenerateReportCommand.Execute(null);
        await Task.Delay(500);

        vm.ReportEmptyMessage.Should().Be("Нет данных за выбранный период");
        vm.IsReportEmptyMessageVisible.Should().BeTrue();
    }

    [Fact]
    public void BeforeFirstReport_ShowsPromptMessage()
    {
        var (mockPython, dbPath) = CreatePythonServiceAndPath();
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);

        vm.HasReportBeenRequested.Should().BeFalse();
        vm.ReportPromptMessage.Should().Contain("Создать отчёт");
        vm.IsReportPromptVisible.Should().BeTrue();
    }

    [Fact]
    public async Task AfterFirstReport_HidesPromptMessage()
    {
        var (mockPython, dbPath) = CreatePythonServiceAndPath();
        var mockRepo = new Mock<ITransactionRepository>();
        mockRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<Transaction>());
        var vm = new MainViewModel(mockRepo.Object, mockPython.Object, dbPath);

        vm.GenerateReportCommand.Execute(null);
        await Task.Delay(500);

        vm.HasReportBeenRequested.Should().BeTrue();
        vm.ReportPromptMessage.Should().BeEmpty();
        vm.IsReportPromptVisible.Should().BeFalse();
    }
}
