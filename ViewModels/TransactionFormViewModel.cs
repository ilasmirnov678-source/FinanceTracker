using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Models;
using FinanceTracker.Services;

namespace FinanceTracker.ViewModels;

// ViewModel формы добавления транзакции.
public partial class TransactionFormViewModel : ObservableObject
{
    private readonly TransactionRepository _repository;
    // Callback для закрытия окна с результатом (true — сохранено, false — отмена).
    private readonly Action<bool?>? _onCloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime? _date;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private decimal _amount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _dateError = string.Empty;

    [ObservableProperty]
    private string _amountError = string.Empty;

    [ObservableProperty]
    private string _categoryError = string.Empty;

    public TransactionFormViewModel(TransactionRepository repository, Action<bool?>? onCloseRequested = null)
    {
        _repository = repository;
        _onCloseRequested = onCloseRequested;
        _date = DateTime.Now.Date;
        _dateError = string.Empty;
        _amountError = "Сумма должна быть больше 0";
        _categoryError = "Укажите категорию";
    }

    // Обновить сообщение об ошибке при изменении даты.
    partial void OnDateChanged(DateTime? value)
    {
        DateError = value.HasValue ? string.Empty : "Укажите дату";
    }

    // Обновить сообщение об ошибке при изменении суммы.
    partial void OnAmountChanged(decimal value)
    {
        AmountError = value > 0 ? string.Empty : "Сумма должна быть больше 0";
    }

    // Обновить сообщение об ошибке при изменении категории.
    partial void OnCategoryChanged(string value)
    {
        CategoryError = !string.IsNullOrWhiteSpace(value) ? string.Empty : "Укажите категорию";
    }

    // Сохранить транзакцию в БД и закрыть форму.
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var transaction = new Transaction
        {
            Date = Date!.Value,
            Amount = Amount,
            Category = Category.Trim(),
            Description = Description?.Trim() ?? string.Empty
        };
        _repository.Add(transaction);
        _onCloseRequested?.Invoke(true);
    }

    // Отменить без сохранения.
    [RelayCommand]
    private void Cancel()
    {
        _onCloseRequested?.Invoke(false);
    }

    // Форма валидна: дата указана, сумма > 0, категория не пуста.
    private bool CanSave() => Date.HasValue && Amount > 0 && !string.IsNullOrWhiteSpace(Category);
}
