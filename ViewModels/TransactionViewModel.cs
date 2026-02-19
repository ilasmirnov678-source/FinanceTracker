using CommunityToolkit.Mvvm.ComponentModel;
using FinanceTracker.Models;

namespace FinanceTracker.ViewModels;

// Обёртка Transaction для отображения и двусторонней привязки в UI.
public partial class TransactionViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    // Id транзакции; только чтение.
    public int Id { get; }

    // Исходная модель (используется репозиторием при обновлении).
    public Transaction Model { get; }

    public TransactionViewModel(Transaction transaction)
    {
        Model = transaction;
        Id = transaction.Id;
        _date = transaction.Date;
        _amount = transaction.Amount;
        _category = transaction.Category;
        _description = transaction.Description;
    }

    // Сумма в формате отображения в списке (например "1 234,56 руб.").
    public string DisplayAmount => $"{Amount:N2} руб.";

    partial void OnDateChanged(DateTime value) => Model.Date = value;
    partial void OnAmountChanged(decimal value) => Model.Amount = value;
    partial void OnCategoryChanged(string value) => Model.Category = value ?? string.Empty;
    partial void OnDescriptionChanged(string value) => Model.Description = value ?? string.Empty;
}
