using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MonitorMate.Converters;

public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && parameter is not null && string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is not null && targetType.IsEnum)
        {
            return Enum.Parse(targetType, parameter.ToString()!, ignoreCase: true);
        }

        return Binding.DoNothing;
    }
}

public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class SliderFillConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 4 ||
            !double.TryParse(values[0]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            !double.TryParse(values[1]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var minimum) ||
            !double.TryParse(values[2]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var maximum) ||
            !double.TryParse(values[3]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var width))
        {
            return 0d;
        }

        if (maximum <= minimum || width <= 0)
        {
            return 0d;
        }

        return Math.Clamp((value - minimum) / (maximum - minimum) * width, 0, width);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}

public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
