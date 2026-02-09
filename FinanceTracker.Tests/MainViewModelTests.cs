using FinanceTracker.Models;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinanceTracker.Tests;

public class MainViewModelTests
{
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

        var vm = new MainViewModel(mockRepo.Object);

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

        var vm = new MainViewModel(mockRepo.Object);
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

        var vm = new MainViewModel(mockRepo.Object);
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

        var vm = new MainViewModel(mockRepo.Object);
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

        var vm = new MainViewModel(mockRepo.Object);
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

        var vm = new MainViewModel(mockRepo.Object);
        mockRepo.Invocations.Clear();

        vm.EndDateFilter = new DateTime(2025, 2, 28);

        mockRepo.Verify(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.AtLeastOnce);
    }
}
