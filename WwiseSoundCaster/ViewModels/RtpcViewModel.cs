using CommunityToolkit.Mvvm.ComponentModel;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Represents a single RTPC (Game Parameter) control.
/// The Slider in the UI binds TwoWay to <see cref="CurrentValue"/>,
/// and value changes should propagate to the WAAPI service layer.
/// </summary>
public partial class RtpcViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private double _minValue;

    [ObservableProperty]
    private double _maxValue = 100.0;

    [ObservableProperty]
    private double _currentValue;
}
