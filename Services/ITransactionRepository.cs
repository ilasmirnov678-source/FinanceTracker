using FinanceTracker.Models;
namespace FinanceTracker.Services;

// Контракт репозитория транзакций; подмена реализации в тестах.
public interface ITransactionRepository
{
    List<Transaction> GetAll();
    List<Transaction> GetByDateRange(DateTime from, DateTime to);
    void Add(Transaction transaction);
    void Update(Transaction transaction);
    void Delete(int id);
}
