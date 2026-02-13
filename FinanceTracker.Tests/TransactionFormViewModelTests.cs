using FinanceTracker.Models;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinanceTracker.Tests;

public class TransactionFormViewModelTests
{
    [Fact]
    public void Constructor_AddMode_SetsDateToToday()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var vm = new TransactionFormViewModel(mockRepo.Object);

        vm.Date.Should().NotBeNull();
        vm.Date!.Value.Date.Should().Be(DateTime.Today);
        vm.Amount.Should().BeNull();
        vm.Category.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_EditMode_PopulatesFromExistingTransaction()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var existing = new Transaction
        {
            Id = 42,
            Date = new DateTime(2025, 2, 15),
            Amount = 500.75m,
            Category = "Продукты",
            Description = "Тестовая запись"
        };

        var vm = new TransactionFormViewModel(mockRepo.Object, null, existing);

        vm.Date.Should().Be(new DateTime(2025, 2, 15));
        vm.Amount.Should().Be(500.75m);
        vm.Category.Should().Be("Продукты");
        vm.Description.Should().Be("Тестовая запись");
    }

    [Fact]
    public void Save_WithInvalidData_SetsErrorMessages()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var vm = new TransactionFormViewModel(mockRepo.Object)
        {
            Date = null,
            Amount = 0,
            Category = ""
        };

        vm.SaveCommand.Execute(null);

        vm.DateError.Should().Be("Укажите дату");
        vm.AmountError.Should().Be("Сумма должна быть больше 0");
        vm.CategoryError.Should().Be("Укажите категорию");
        mockRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public void Save_WithValidData_CallsAdd_AndInvokesCallback()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        bool? callbackResult = null;
        var vm = new TransactionFormViewModel(mockRepo.Object, (r, _) => callbackResult = r)
        {
            Date = new DateTime(2025, 2, 15),
            Amount = 100,
            Category = "Продукты",
            Description = "Тест"
        };

        vm.SaveCommand.Execute(null);

        mockRepo.Verify(r => r.Add(It.Is<Transaction>(t =>
            t.Date == new DateTime(2025, 2, 15) &&
            t.Amount == 100 &&
            t.Category == "Продукты" &&
            t.Description == "Тест")), Times.Once);
        callbackResult.Should().BeTrue();
    }

    [Fact]
    public void Save_EditMode_CallsUpdate_AndInvokesCallback()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var existing = new Transaction
        {
            Id = 10,
            Date = new DateTime(2025, 2, 1),
            Amount = 50,
            Category = "A",
            Description = "Old"
        };
        bool? callbackResult = null;
        var vm = new TransactionFormViewModel(mockRepo.Object, (r, _) => callbackResult = r, existing)
        {
            Amount = 75,
            Category = "B",
            Description = "Updated"
        };

        vm.SaveCommand.Execute(null);

        mockRepo.Verify(r => r.Update(It.Is<Transaction>(t =>
            t.Id == 10 &&
            t.Amount == 75 &&
            t.Category == "B" &&
            t.Description == "Updated")), Times.Once);
        mockRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
        callbackResult.Should().BeTrue();
    }

    [Fact]
    public void Cancel_InvokesCallbackWithFalse()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        bool? callbackResult = null;
        var vm = new TransactionFormViewModel(mockRepo.Object, (r, _) => callbackResult = r);

        vm.CancelCommand.Execute(null);

        callbackResult.Should().BeFalse();
        mockRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public void Save_WhenAmountZero_DoesNotCallAdd()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var vm = new TransactionFormViewModel(mockRepo.Object)
        {
            Date = DateTime.Today,
            Amount = 0,
            Category = "Test"
        };

        vm.SaveCommand.Execute(null);

        mockRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
        vm.AmountError.Should().Be("Сумма должна быть больше 0");
    }

    [Fact]
    public void Save_WhenAmountNegative_DoesNotCallAdd()
    {
        var mockRepo = new Mock<ITransactionRepository>();
        var vm = new TransactionFormViewModel(mockRepo.Object)
        {
            Date = DateTime.Today,
            Amount = -10,
            Category = "Test"
        };

        vm.SaveCommand.Execute(null);

        mockRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
        vm.AmountError.Should().Be("Сумма должна быть больше 0");
    }
}
