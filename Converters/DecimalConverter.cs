using System.Globalization;
using System.Windows.Data;

namespace FinanceTracker.Converters;

// Привязка TextBox к decimal?; пустая строка трактуется как null.
public class DecimalConverter : IValueConverter
{
    // Преобразовать decimal? в строку для отображения в TextBox.
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d.ToString(CultureInfo.InvariantCulture);
        return string.Empty;
    }

    // Преобразовать ввод в decimal?; пустая строка — null. Позволяет очистить поле.
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return null;
        var trimmed = s.Trim();
        // Не обновлять источник при незавершённом вводе (например "10." или "10,").
        if (trimmed.EndsWith('.') || trimmed.EndsWith(','))
            return Binding.DoNothing;
        if (decimal.TryParse(s.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }
}
