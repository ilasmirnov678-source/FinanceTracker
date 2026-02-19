using System.Text.Json.Serialization;

namespace FinanceTracker.Models.Analytics;

// Элемент сводки по месяцу; контракт JSON: month, sum.
public class MonthSum
{
    [JsonPropertyName("month")]
    public string Month { get; set; } = string.Empty;

    [JsonPropertyName("sum")]
    public double Sum { get; set; }
}
