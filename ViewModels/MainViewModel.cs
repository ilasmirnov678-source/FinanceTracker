using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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
}
