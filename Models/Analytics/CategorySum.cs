using System.Text.Json.Serialization;

namespace FinanceTracker.Models.Analytics;

// Элемент сводки по категории; контракт JSON: name, sum.
public class CategorySum
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sum")]
    public double Sum { get; set; }
}
