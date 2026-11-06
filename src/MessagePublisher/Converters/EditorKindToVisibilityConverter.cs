using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MessagePublisher.ViewModels;

namespace MessagePublisher.Converters;

public sealed class EditorKindToVisibilityConverter : IValueConverter
{
    public PropertyEditorKind MatchKind { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PropertyEditorKind k && k == MatchKind)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
