using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Services;
using FinanceTracker.Views;

namespace FinanceTracker.ViewModels;

// ViewModel главного окна.
public partial class MainViewModel : ObservableObject
{
    private readonly ITransactionRepository _repository;

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

    public MainViewModel(ITransactionRepository repository)
    {
        _repository = repository;
        Transactions = new ObservableCollection<TransactionViewModel>();
        _startDateFilter = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        _endDateFilter = DateTime.Now.Date;
        RefreshCommand.Execute(null);
    }

    // Добавить транзакцию.
    [RelayCommand]
    private void AddTransaction()
    {
        var window = new TransactionFormWindow(_repository)
        {
            Owner = Application.Current.MainWindow
        };
        if (window.ShowDialog() == true)
            RefreshCommand.Execute(null);
    }

    // Редактировать выбранную транзакцию.
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void EditTransaction()
    {
        if (SelectedTransaction == null) return;
        var window = new TransactionFormWindow(_repository, SelectedTransaction.Model)
        {
            Owner = Application.Current.MainWindow
        };
        if (window.ShowDialog() == true)
            RefreshCommand.Execute(null);
    }

    // Удалить выбранную транзакцию.
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void DeleteTransaction()
    {
        if (SelectedTransaction == null) return;
        if (MessageBox.Show(
                $"Удалить транзакцию от {SelectedTransaction.Date:dd.MM.yyyy} ({SelectedTransaction.Amount:N2} руб., {SelectedTransaction.Category})?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _repository.Delete(SelectedTransaction.Id);
        RefreshCommand.Execute(null);
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

    partial void OnStartDateFilterChanged(DateTime value) => RefreshCommand.Execute(null);
    partial void OnEndDateFilterChanged(DateTime value) => RefreshCommand.Execute(null);
}
