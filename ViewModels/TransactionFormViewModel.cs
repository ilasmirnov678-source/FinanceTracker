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
    // Id существующей транзакции при редактировании; null при добавлении.
    private readonly int? _existingId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime? _date;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private decimal? _amount;

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

    public TransactionFormViewModel(TransactionRepository repository, Action<bool?>? onCloseRequested = null, Transaction? existingTransaction = null)
    {
        _repository = repository;
        _onCloseRequested = onCloseRequested;
        _existingId = existingTransaction?.Id;

        if (existingTransaction != null)
        {
            _date = existingTransaction.Date;
            _amount = existingTransaction.Amount;
            _category = existingTransaction.Category;
            _description = existingTransaction.Description ?? string.Empty;
        }
        else
        {
            _date = DateTime.Now.Date;
        }

        _dateError = string.Empty;
        _amountError = string.Empty;
        _categoryError = string.Empty;
    }

    // Очистить ошибку даты при исправлении поля.
    partial void OnDateChanged(DateTime? value)
    {
        if (value.HasValue)
            DateError = string.Empty;
    }

    // Очистить ошибку суммы при исправлении поля.
    partial void OnAmountChanged(decimal? value)
    {
        if (value.HasValue && value.Value > 0)
            AmountError = string.Empty;
    }

    // Очистить ошибку категории при исправлении поля.
    partial void OnCategoryChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            CategoryError = string.Empty;
    }

    // Сохранить транзакцию в БД и закрыть форму.
    [RelayCommand]
    private void Save()
    {
        // Валидация при попытке сохранения.
        DateError = Date.HasValue ? string.Empty : "Укажите дату";
        AmountError = Amount.HasValue && Amount.Value > 0 ? string.Empty : "Сумма должна быть больше 0";
        CategoryError = !string.IsNullOrWhiteSpace(Category) ? string.Empty : "Укажите категорию";

        if (!CanSave())
            return;

        var transaction = new Transaction
        {
            Id = _existingId ?? 0,
            Date = Date!.Value,
            Amount = Amount!.Value,
            Category = Category.Trim(),
            Description = Description?.Trim() ?? string.Empty
        };

        if (_existingId.HasValue)
            _repository.Update(transaction);
        else
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
    private bool CanSave() => Date.HasValue && Amount.HasValue && Amount.Value > 0 && !string.IsNullOrWhiteSpace(Category);
}
