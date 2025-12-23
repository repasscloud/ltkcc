using System.Globalization;

namespace LTKCC.Converters;

public static class StringConverters
{
    public static readonly IValueConverter NotNullOrEmpty = new NotNullOrEmptyConverter();

    private sealed class NotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string s && !string.IsNullOrWhiteSpace(s);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
