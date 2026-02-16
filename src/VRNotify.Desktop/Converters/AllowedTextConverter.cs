using System.Globalization;
using System.Windows.Data;

namespace VRNotify.Desktop.Converters;

public sealed class AllowedTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "Allowed" : "Blocked";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
