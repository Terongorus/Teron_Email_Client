using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TeronEmailClient.Converters;

public sealed class HexColorToBrushConverter : IValueConverter
{
    public static readonly HexColorToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return new BrushConverter().ConvertFromString(hex) ?? Brushes.Gray;
            }
            catch (FormatException)
            {
                return Brushes.Gray;
            }
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
