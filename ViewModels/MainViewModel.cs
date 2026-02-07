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

    public MainViewModel(TransactionRepository repository)
    {
        _repository = repository;
        Transactions = new ObservableCollection<TransactionViewModel>();
    }
}
