using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Root ViewModel for the main application window.
/// Drives both the left-panel Event Browser and the right-panel control area.
/// All WAAPI interactions should be injected via a service — no business logic here.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    // ── Left Panel ──────────────────────────────────────────────

    /// <summary>
    /// Hierarchical collection of event nodes displayed in the TreeView.
    /// </summary>
    public ObservableCollection<EventNodeViewModel> EventTreeNodes { get; } = new();

    /// <summary>
    /// Currently selected node in the event tree. Drives the right-panel content.
    /// </summary>
    [ObservableProperty]
    private EventNodeViewModel? _selectedEventNode;

    /// <summary>
    /// Search filter text bound to the search bar.
    /// </summary>
    [ObservableProperty]
    private string _searchKeywords = string.Empty;

    // ── Right Panel – Header ────────────────────────────────────

    /// <summary>
    /// Display name of the currently selected event (derived from SelectedEventNode).
    /// </summary>
    public string EventName => SelectedEventNode?.Name ?? string.Empty;

    // Notify EventName when SelectedEventNode changes
    partial void OnSelectedEventNodeChanged(EventNodeViewModel? value)
    {
        OnPropertyChanged(nameof(EventName));
        OnPropertyChanged(nameof(HasSelectedEvent));
    }

    /// <summary>
    /// True when an event is selected — used to toggle right-panel visibility.
    /// </summary>
    public bool HasSelectedEvent => SelectedEventNode is not null;

    // ── Right Panel – RTPC & Switch Collections ─────────────────

    /// <summary>
    /// RTPC parameters for the currently selected event.
    /// </summary>
    public ObservableCollection<RtpcViewModel> CurrentEventRTPCs { get; } = new();

    /// <summary>
    /// Switch groups for the currently selected event.
    /// </summary>
    public ObservableCollection<SwitchGroupViewModel> CurrentEventSwitches { get; } = new();

    // ── Right Panel – Status Bar ────────────────────────────────

    /// <summary>
    /// Bottom status bar message (e.g. connection state).
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Disconnected";

    /// <summary>
    /// Indicates whether the application is connected to Wwise WAAPI.
    /// Used by the UI converter to colour the status bar.
    /// </summary>
    [ObservableProperty]
    private bool _isConnected;

    // ── Commands ────────────────────────────────────────────────

    /// <summary>
    /// Posts the selected event to the Wwise sound engine.
    /// </summary>
    [RelayCommand]
    private void PlayEvent()
    {
        // TODO: call WwiseService.PostEvent(SelectedEventNode.Id)
    }

    /// <summary>
    /// Stops all currently playing sounds.
    /// </summary>
    [RelayCommand]
    private void StopEvent()
    {
        // TODO: call WwiseService.StopAll()
    }
}