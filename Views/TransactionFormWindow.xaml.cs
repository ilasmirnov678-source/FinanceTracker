using System.Windows;
using FinanceTracker.Models;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;

namespace FinanceTracker.Views;

// Окно добавления или редактирования одной транзакции.
public partial class TransactionFormWindow : Window
{
    // Результат при успешном сохранении; null при отмене.
    public Transaction? SavedTransaction { get; private set; }

    public TransactionFormWindow(ITransactionRepository repository, Transaction? existingTransaction = null)
    {
        InitializeComponent();
        DataContext = new TransactionFormViewModel(repository, OnCloseRequested, existingTransaction);
    }

    // Обработать закрытие: сохранить результат и транзакцию в свойства, закрыть окно (вызов из ViewModel).
    private void OnCloseRequested(bool? result, Transaction? savedTransaction)
    {
        SavedTransaction = savedTransaction;
        DialogResult = result;
        Close();
    }
}
