using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Services;

namespace FinanceTracker.ViewModels;

// ViewModel главного окна.
public partial class MainViewModel : ObservableObject
{
    private readonly TransactionRepository _repository;

    // Коллекция транзакций для отображения в UI.
    public ObservableCollection<TransactionViewModel> Transactions { get; }

    // Выбранная транзакция в списке (для редактирования и удаления).
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditTransactionCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteTransactionCommand))]
    private TransactionViewModel? _selectedTransaction;

    // Начало периода для фильтрации.
    [ObservableProperty]
    private DateTime _startDateFilter;

    // Конец периода для фильтрации.
    [ObservableProperty]
    private DateTime _endDateFilter;

    public MainViewModel(TransactionRepository repository)
    {
        _repository = repository;
        Transactions = new ObservableCollection<TransactionViewModel>();
        _startDateFilter = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        _endDateFilter = DateTime.Now.Date;
    }

    // Добавить транзакцию (реализация в следующем коммите).
    [RelayCommand]
    private void AddTransaction()
    {
    }

    // Редактировать выбранную транзакцию (реализация в следующем коммите).
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void EditTransaction()
    {
    }

    // Удалить выбранную транзакцию (реализация в следующем коммите).
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void DeleteTransaction()
    {
    }

    // Обновить список транзакций.
    [RelayCommand]
    private void Refresh()
    {
        Transactions.Clear();
        var transactions = _repository.GetByDateRange(StartDateFilter, EndDateFilter);
        foreach (var t in transactions)
            Transactions.Add(new TransactionViewModel(t));
    }

    private bool CanEditOrDelete() => SelectedTransaction != null;
}
