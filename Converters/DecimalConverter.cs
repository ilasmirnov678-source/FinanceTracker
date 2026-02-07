using System.Globalization;
using System.Windows.Data;

namespace FinanceTracker.Converters;

// Конвертер для привязки TextBox к decimal.
public class DecimalConverter : IValueConverter
{
    // decimal -> string для отображения в TextBox.
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d.ToString(CultureInfo.InvariantCulture);
        return string.Empty;
    }

    // string -> decimal при вводе; при некорректном вводе возвращает 0.
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && decimal.TryParse(s.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0m;
    }
}
