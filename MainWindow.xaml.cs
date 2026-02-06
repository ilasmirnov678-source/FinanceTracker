using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FinanceTracker.Services;
using FinanceTracker.Models;

namespace FinanceTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dbService = new DatabaseService();
                var repository = new TransactionRepository(dbService.ConnectionString);

                // Тест добавления
                var transaction = new Transaction
                {
                    Date = DateTime.Now,
                    Amount = 50.25m,
                    Category = "Еда",
                    Description = "Обед"
                };
                repository.Add(transaction);

                // Тест получения
                var all = repository.GetAll();
                MessageBox.Show($"Тест успешен!\nВсего транзакций: {all.Count}\nПоследняя: {all.First().Category} - {all.First().Amount} руб.", 
                    "Результат теста", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка теста", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}