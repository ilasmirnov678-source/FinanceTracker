using System.Windows;
using FinanceTracker.Models;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;

namespace FinanceTracker.Views;

// Окно добавления или редактирования транзакции.
public partial class TransactionFormWindow : Window
{
    public TransactionFormWindow(ITransactionRepository repository, Transaction? existingTransaction = null)
    {
        InitializeComponent();
        DataContext = new TransactionFormViewModel(repository, OnCloseRequested, existingTransaction);
    }

    // Установить DialogResult и закрыть окно (вызывается из ViewModel).
    private void OnCloseRequested(bool? result)
    {
        DialogResult = result;
        Close();
    }
}
