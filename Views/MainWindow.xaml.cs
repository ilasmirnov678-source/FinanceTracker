using System.Windows;
using System.Windows.Input;
using FinanceTracker.Services;
using FinanceTracker.ViewModels;

namespace FinanceTracker.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var dbService = new DatabaseService();
            var repository = new TransactionRepository(dbService.ConnectionString);
            var pythonService = new PythonService();
            DataContext = new MainViewModel(repository, pythonService, dbService.DbPath);
            StateChanged += (_, _) => UpdateMaximizeRestoreButtonContent();
            Loaded += (_, _) => UpdateMaximizeRestoreButtonContent();
        }

        private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    ApplyRestoredMaximizedBounds();
                }
                else
                    WindowState = WindowState.Maximized;
                UpdateMaximizeRestoreButtonContent();
                return;
            }
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                ApplyRestoredMaximizedBounds();
                UpdateMaximizeRestoreButtonContent();
            }
            DragMove();
        }

        private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                ApplyRestoredMaximizedBounds();
            }
            else
                WindowState = WindowState.Maximized;
            UpdateMaximizeRestoreButtonContent();
        }

        private void ApplyRestoredMaximizedBounds()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;
        }

        private void UpdateMaximizeRestoreButtonContent()
        {
            MaximizeRestoreButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
            MaximizeRestoreButton.ToolTip = WindowState == WindowState.Maximized ? "Восстановить" : "Развернуть";
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}