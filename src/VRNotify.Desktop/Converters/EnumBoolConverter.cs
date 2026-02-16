using System.Globalization;
using System.Windows.Data;

namespace VRNotify.Desktop.Converters;

public sealed class EnumBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string enumStr)
            return Enum.Parse(targetType, enumStr);
        return Binding.DoNothing;
    }
}
