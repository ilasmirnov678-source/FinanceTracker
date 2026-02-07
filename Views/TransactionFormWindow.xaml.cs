using System.Windows;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;

namespace FinanceTracker.Views;

// Окно добавления новой транзакции.
public partial class TransactionFormWindow : Window
{
    public TransactionFormWindow(TransactionRepository repository)
    {
        InitializeComponent();
        DataContext = new TransactionFormViewModel(repository, OnCloseRequested);
    }

    // Установить DialogResult и закрыть окно (вызывается из ViewModel).
    private void OnCloseRequested(bool? result)
    {
        DialogResult = result;
        Close();
    }
}
