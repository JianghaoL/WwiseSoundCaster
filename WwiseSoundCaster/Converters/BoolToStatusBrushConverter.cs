using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WwiseSoundCaster.Converters;

/// <summary>
/// Converts a boolean connection state to a status bar background brush.
/// true  → dark green (#1B5E20)
/// false → dark red   (#B71C1C)
/// </summary>
public class BoolToStatusBrushConverter : IValueConverter
{
    public static readonly BoolToStatusBrushConverter Instance = new();

    private static readonly IBrush ConnectedBrush = new SolidColorBrush(Color.Parse("#1B5E20"));
    private static readonly IBrush DisconnectedBrush = new SolidColorBrush(Color.Parse("#B71C1C"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? ConnectedBrush : DisconnectedBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
