using System.Windows;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;

namespace FinanceTracker.Views;

public partial class TransactionFormWindow : Window
{
    public TransactionFormWindow(TransactionRepository repository)
    {
        InitializeComponent();
        DataContext = new TransactionFormViewModel(repository, OnCloseRequested);
    }

    private void OnCloseRequested(bool? result)
    {
        DialogResult = result;
        Close();
    }
}
