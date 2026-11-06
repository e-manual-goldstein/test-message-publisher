using System.Globalization;
using System.Windows.Data;

namespace MessagePublisher.Converters;

/// <summary>
/// Inverts boolean (e.g. IsBusy true → IsEnabled false).
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
