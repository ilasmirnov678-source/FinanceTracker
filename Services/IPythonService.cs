using FinanceTracker.Models.Analytics;

namespace FinanceTracker.Services;

public interface IPythonService
{
    Task<AnalyticsResult> GenerateReportAsync(string dbPath, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
