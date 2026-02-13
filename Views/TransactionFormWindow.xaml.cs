using System.Windows;
using FinanceTracker.Models;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;

namespace FinanceTracker.Views;

// Окно добавления или редактирования транзакции.
public partial class TransactionFormWindow : Window
{
    // Сохранённая транзакция при успешном Save; null при отмене.
    public Transaction? SavedTransaction { get; private set; }

    public TransactionFormWindow(ITransactionRepository repository, Transaction? existingTransaction = null)
    {
        InitializeComponent();
        DataContext = new TransactionFormViewModel(repository, OnCloseRequested, existingTransaction);
    }

    // Установить SavedTransaction, DialogResult и закрыть окно (вызывается из ViewModel).
    private void OnCloseRequested(bool? result, Transaction? savedTransaction)
    {
        SavedTransaction = savedTransaction;
        DialogResult = result;
        Close();
    }
}
