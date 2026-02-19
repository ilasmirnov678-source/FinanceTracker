using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Models;
using FinanceTracker.Models.Analytics;
using FinanceTracker.Services;
using FinanceTracker.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace FinanceTracker.ViewModels;

public class ReportChartTypeItem(ReportChartType value, string display)
{
    public ReportChartType Value { get; } = value;
    public string Display { get; } = display;
}

public partial class MainViewModel : ObservableObject
{
    private readonly ITransactionRepository _repository;
    private readonly IPythonService _pythonService;
    private readonly string _dbPath;

    public IReadOnlyList<ReportChartTypeItem> ReportChartTypeItems { get; } =
    [
        new ReportChartTypeItem(ReportChartType.ByCategory, "По категориям (круговая)"),
        new ReportChartTypeItem(ReportChartType.ByMonth, "По месяцам (столбцы)"),
        new ReportChartTypeItem(ReportChartType.Both, "Оба")
    ];

    public ObservableCollection<TransactionViewModel> Transactions { get; }

    // Текущая выбранная транзакция в списке (редактирование, удаление).
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditTransactionCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteTransactionCommand))]
    private TransactionViewModel? _selectedTransaction;

    // Выбранные транзакции (заполняется из MainWindow при SelectionChanged).
    private List<TransactionViewModel> _selectedTransactions = new();
    public IReadOnlyList<TransactionViewModel> SelectedTransactions => _selectedTransactions;

    public int SelectedTransactionsCount => _selectedTransactions.Count;
    public bool HasSingleSelection => SelectedTransactionsCount == 1;
    public bool HasAnySelection => SelectedTransactionsCount > 0;

    public void SetSelectedTransactions(IEnumerable<TransactionViewModel> selected)
    {
        _selectedTransactions = selected?.ToList() ?? new List<TransactionViewModel>();
        OnPropertyChanged(nameof(SelectedTransactions));
        OnPropertyChanged(nameof(SelectedTransactionsCount));
        OnPropertyChanged(nameof(HasSingleSelection));
        OnPropertyChanged(nameof(HasAnySelection));
        (EditTransactionCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (DeleteTransactionCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    // Начало и конец периода фильтрации списка транзакций.
    [ObservableProperty]
    private DateTime _startDateFilter;
    [ObservableProperty]
    private DateTime _endDateFilter;

    // Начало и конец периода для отчёта (графики).
    [ObservableProperty]
    private DateTime _reportStartDate;
    [ObservableProperty]
    private DateTime _reportEndDate;

    // Серии круговой диаграммы по категориям.
    public ObservableCollection<ISeries> CategoryChartSeries { get; } = new();
    // Серии столбчатой диаграммы по месяцам.
    public ObservableCollection<ISeries> MonthChartSeries { get; } = new();
    // Подписи оси X графика по месяцам (формат yyyy-MM).
    public ObservableCollection<Axis> MonthChartXAxes { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool _isReportGenerating;

    [ObservableProperty]
    private string _reportError = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCategoryChartVisible))]
    [NotifyPropertyChangedFor(nameof(IsMonthChartVisible))]
    [NotifyPropertyChangedFor(nameof(IsChartTypeByCategory))]
    [NotifyPropertyChangedFor(nameof(IsChartTypeByMonth))]
    [NotifyPropertyChangedFor(nameof(IsChartTypeBoth))]
    private ReportChartTypeItem? _selectedReportChartTypeItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReportPromptMessage))]
    [NotifyPropertyChangedFor(nameof(IsReportPromptVisible))]
    private bool _hasReportBeenRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReportEmptyMessageVisible))]
    private string _reportEmptyMessage = string.Empty;

    public string ReportPromptMessage => HasReportBeenRequested ? string.Empty : "Выберите период и нажмите «Создать отчёт»";

    public bool IsReportPromptVisible => !HasReportBeenRequested;

    public bool IsReportEmptyMessageVisible => !string.IsNullOrEmpty(ReportEmptyMessage);

    public bool IsCategoryChartVisible =>
        SelectedReportChartTypeItem?.Value == ReportChartType.ByCategory || SelectedReportChartTypeItem?.Value == ReportChartType.Both;

    public bool IsMonthChartVisible =>
        SelectedReportChartTypeItem?.Value == ReportChartType.ByMonth || SelectedReportChartTypeItem?.Value == ReportChartType.Both;

    public bool IsChartTypeByCategory
    {
        get => SelectedReportChartTypeItem?.Value == ReportChartType.ByCategory;
        set { if (value) SelectedReportChartTypeItem = ReportChartTypeItems.First(x => x.Value == ReportChartType.ByCategory); }
    }

    public bool IsChartTypeByMonth
    {
        get => SelectedReportChartTypeItem?.Value == ReportChartType.ByMonth;
        set { if (value) SelectedReportChartTypeItem = ReportChartTypeItems.First(x => x.Value == ReportChartType.ByMonth); }
    }

    public bool IsChartTypeBoth
    {
        get => SelectedReportChartTypeItem?.Value == ReportChartType.Both;
        set { if (value) SelectedReportChartTypeItem = ReportChartTypeItems.First(x => x.Value == ReportChartType.Both); }
    }

    // Признак пустого списка за период (заглушка в левой панели).
    public bool IsTransactionsEmpty => Transactions.Count == 0;

    public bool HasTransactions => !IsTransactionsEmpty;

    public MainViewModel(ITransactionRepository repository, IPythonService pythonService, string dbPath)
    {
        _repository = repository;
        _pythonService = pythonService;
        _dbPath = dbPath;
        Transactions = new ObservableCollection<TransactionViewModel>();
        Transactions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsTransactionsEmpty));
            OnPropertyChanged(nameof(HasTransactions));
        };
        _startDateFilter = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        _endDateFilter = DateTime.Now.Date;
        _reportStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        _reportEndDate = DateTime.Now.Date;
        _selectedReportChartTypeItem = ReportChartTypeItems.First(x => x.Value == ReportChartType.Both);
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
        {
            if (window.SavedTransaction is { } t)
                ExpandDateFilterIfNeeded(t.Date);
            RefreshCommand.Execute(null);
        }
    }

    // Редактировать выбранную транзакцию.
    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void EditTransaction()
    {
        if (SelectedTransaction == null) return;
        var window = new TransactionFormWindow(_repository, SelectedTransaction.Model)
        {
            Owner = Application.Current.MainWindow
        };
        if (window.ShowDialog() == true)
        {
            if (window.SavedTransaction is { } t)
                ExpandDateFilterIfNeeded(t.Date);
            RefreshCommand.Execute(null);
        }
    }

    // Удалить выбранные транзакции (одну или несколько).
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void DeleteTransaction()
    {
        if (SelectedTransactionsCount == 0) return;
        string message;
        if (SelectedTransactionsCount == 1)
        {
            var t = _selectedTransactions[0];
            message = $"Удалить транзакцию от {t.Date:dd.MM.yyyy} ({t.Amount:N2} руб., {t.Category})?";
        }
        else
            message = $"Удалить транзакции ({SelectedTransactionsCount})?";
        if (MessageBox.Show(message, "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        foreach (var t in _selectedTransactions)
            _repository.Delete(t.Id);
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

    private bool CanEdit() => HasSingleSelection;
    private bool CanDelete() => HasAnySelection;

    // Расширить период фильтрации, если дата транзакции выходит за текущий диапазон (internal для тестов).
    internal void ExpandDateFilterIfNeeded(DateTime transactionDate)
    {
        var date = transactionDate.Date;
        if (date < StartDateFilter)
            StartDateFilter = date;
        if (date > EndDateFilter)
            EndDateFilter = date;
    }

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
            ReportEmptyMessage = string.Empty;
            MessageBox.Show(ex.Message, "Ошибка отчёта", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsReportGenerating = false;
            HasReportBeenRequested = true;
        }
    }

    private bool CanGenerateReport() => !IsReportGenerating;

    // Обновить серии графиков по результату аналитики (категории и месяцы).
    private void UpdateChartSeries(AnalyticsResult result)
    {
        ReportEmptyMessage = result.ByCategory.Count == 0 && result.ByMonth.Count == 0
            ? "Нет данных за выбранный период"
            : string.Empty;

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
