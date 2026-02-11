using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Models.Analytics;
using FinanceTracker.Services;
using FinanceTracker.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace FinanceTracker.ViewModels;

// ViewModel главного окна.
public partial class MainViewModel : ObservableObject
{
    private readonly ITransactionRepository _repository;
    private readonly IPythonService _pythonService;
    private readonly string _dbPath;

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

    // Начало периода для отчёта (графики).
    [ObservableProperty]
    private DateTime _reportStartDate;

    // Конец периода для отчёта (графики).
    [ObservableProperty]
    private DateTime _reportEndDate;

    // Серии для круговой диаграммы по категориям.
    public ObservableCollection<ISeries> CategoryChartSeries { get; } = new();

    // Серии для столбчатой диаграммы по месяцам.
    public ObservableCollection<ISeries> MonthChartSeries { get; } = new();

    // Подписи оси X для графика по месяцам (yyyy-MM).
    public ObservableCollection<Axis> MonthChartXAxes { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool _isReportGenerating;

    [ObservableProperty]
    private string _reportError = string.Empty;

    public MainViewModel(ITransactionRepository repository, IPythonService pythonService, string dbPath)
    {
        _repository = repository;
        _pythonService = pythonService;
        _dbPath = dbPath;
        Transactions = new ObservableCollection<TransactionViewModel>();
        _startDateFilter = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        _endDateFilter = DateTime.Now.Date;
        _reportStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        _reportEndDate = DateTime.Now.Date;
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

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private async Task GenerateReportAsync()
    {
        IsReportGenerating = true;
        ReportError = string.Empty;
        try
        {
            var result = await _pythonService.GenerateReportAsync(_dbPath, ReportStartDate, ReportEndDate);
            UpdateChartSeries(result);
        }
        catch (Exception ex)
        {
            ReportError = ex.Message;
            MessageBox.Show(ex.Message, "Ошибка отчёта", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsReportGenerating = false;
        }
    }

    private bool CanGenerateReport() => !IsReportGenerating;

    private void UpdateChartSeries(AnalyticsResult result)
    {
        CategoryChartSeries.Clear();
        foreach (var c in result.ByCategory)
            CategoryChartSeries.Add(new PieSeries<double> { Values = new[] { c.Sum }, Name = c.Name });

        MonthChartSeries.Clear();
        MonthChartXAxes.Clear();
        if (result.ByMonth.Count > 0)
        {
            MonthChartSeries.Add(new ColumnSeries<double>
            {
                Values = result.ByMonth.Select(m => m.Sum).ToArray(),
                Name = "Сумма"
            });
            MonthChartXAxes.Add(new Axis
            {
                Labels = result.ByMonth.Select(m => m.Month).ToArray(),
                LabelsRotation = 0,
                ForceStepToMin = true,
                MinStep = 1
            });
        }
    }

    partial void OnStartDateFilterChanged(DateTime value) => RefreshCommand.Execute(null);
    partial void OnEndDateFilterChanged(DateTime value) => RefreshCommand.Execute(null);
}
