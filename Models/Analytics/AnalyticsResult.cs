using System.Text.Json.Serialization;

namespace FinanceTracker.Models.Analytics;

// Результат аналитики analyzer.py; контракт JSON: by_category, by_month, total.
public class AnalyticsResult
{
    [JsonPropertyName("by_category")]
    public List<CategorySum> ByCategory { get; set; } = new();

    [JsonPropertyName("by_month")]
    public List<MonthSum> ByMonth { get; set; } = new();

    [JsonPropertyName("total")]
    public double Total { get; set; }
}
