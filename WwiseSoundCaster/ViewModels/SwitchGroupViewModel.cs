using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Represents a single Switch Group control.
/// The ComboBox in the UI binds to <see cref="AvailableStates"/> and
/// the selected state propagates to the WAAPI service layer.
/// </summary>
public partial class SwitchGroupViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private string _id = string.Empty;

    /// <summary>
    /// List of available switch state names for the ComboBox.
    /// </summary>
    public ObservableCollection<string> AvailableStates { get; } = new();

    [ObservableProperty]
    private string? _selectedState;
}
