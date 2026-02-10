using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WwiseSoundCaster.ViewModels;

namespace WwiseSoundCaster.Converters;

/// <summary>
/// Converts a <see cref="ConnectionStatus"/> enum value to an appropriate
/// foreground <see cref="IBrush"/> for the status label.
///
/// Mapping:
///   None      → Gray   (#888888)
///   Connected → Green  (#4CAF50)
///   Failed    → Red    (#EF5350)
/// </summary>
public class ConnectionStatusToBrushConverter : IValueConverter
{
    public static readonly ConnectionStatusToBrushConverter Instance = new();

    private static readonly IBrush NoneBrush = new SolidColorBrush(Color.Parse("#888888"));
    private static readonly IBrush ConnectedBrush = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush FailedBrush = new SolidColorBrush(Color.Parse("#EF5350"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ConnectionStatus.Connected => ConnectedBrush,
            ConnectionStatus.Failed => FailedBrush,
            _ => NoneBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
