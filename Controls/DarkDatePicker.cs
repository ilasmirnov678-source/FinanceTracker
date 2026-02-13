using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FinanceTracker.Controls;

/// <summary>
/// DatePicker, у которого выпадающий календарь получает ресурсы приложения (тёмные стили).
/// Popup в WPF отображает содержимое в отдельном окне без Application.Resources,
/// поэтому календарь оборачивается в Border и явно задаётся стиль из Themes/CalendarDark.xaml.
/// </summary>
public class DarkDatePicker : DatePicker
{
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_Popup") is Popup popup)
        {
            WrapPopupContentWithAppResources(popup);
            if (popup.Child == null)
                Loaded += OnLoaded;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (GetTemplateChild("PART_Popup") is Popup popup)
            WrapPopupContentWithAppResources(popup);
    }

    private static void WrapPopupContentWithAppResources(Popup popup)
    {
        if (popup.Child == null || popup.Child is Border)
            return;

        var calendar = popup.Child;
        popup.Child = null;

        var border = new Border
        {
            Background = (Brush)Application.Current.FindResource("CardBackground"),
            BorderBrush = (Brush)Application.Current.FindResource("CardBorderDark"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8),
            Child = calendar
        };

        var calendarTheme = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/FinanceTracker;component/Themes/CalendarDark.xaml", UriKind.Absolute)
        };
        border.Resources = calendarTheme;

        if (calendar is Calendar cal && calendarTheme[typeof(Calendar)] is Style calendarStyle)
            cal.Style = calendarStyle;

        popup.Child = border;

        void ApplyThemeToCalendarParts(object? sender, EventArgs e)
            => ApplyStylesToDescendants(border, calendarTheme);

        popup.Opened += ApplyThemeToCalendarParts;
    }

    private static void ApplyStylesToDescendants(DependencyObject parent, ResourceDictionary theme)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is CalendarItem item && theme[typeof(CalendarItem)] is Style itemStyle)
                item.Style = itemStyle;
            else if (child is CalendarButton btn && theme[typeof(CalendarButton)] is Style btnStyle)
            {
                btn.Style = btnStyle;
                if (theme["CalendarDayText"] is Brush headerBrush)
                    btn.Foreground = headerBrush;
            }
            else if (child is CalendarDayButton dayBtn && theme[typeof(CalendarDayButton)] is Style dayStyle)
                dayBtn.Style = dayStyle;
            else if (child is TextBlock tb && theme["CalendarDayText"] is Brush textBrush && !HasAncestor(tb, typeof(CalendarDayButton)))
                tb.Foreground = textBrush;
            ApplyStylesToDescendants(child, theme);
        }
    }

    private static bool HasAncestor(DependencyObject element, Type ancestorType)
    {
        for (var parent = VisualTreeHelper.GetParent(element); parent != null; parent = VisualTreeHelper.GetParent(parent))
            if (ancestorType.IsInstanceOfType(parent))
                return true;
        return false;
    }
}
