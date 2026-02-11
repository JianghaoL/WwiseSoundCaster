using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WwiseSoundCaster.Models;
using WwiseSoundCaster.Services;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Root ViewModel for the main application window.
/// Drives both the left-panel Event Browser and the right-panel control area.
///
/// Architectural notes:
///   • All Wwise data access goes through <see cref="IWwiseObjectService"/>,
///     which is constructor-injected — no direct WAAPI calls here.
///   • Async initialisation (<see cref="InitializeAsync"/>) is triggered
///     by the <see cref="Services.WindowService"/> after the window is shown,
///     keeping the constructor fast and synchronous.
///   • This ViewModel has <b>no knowledge</b> of the connect window or
///     its ViewModel.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    // ── Dependencies ────────────────────────────────────────────

    private readonly IWwiseObjectService _objectService;

    // ── Constructor ─────────────────────────────────────────────

    public MainWindowViewModel(IWwiseObjectService objectService)
    {
        _objectService = objectService;
    }

    // ── Async Initialisation ────────────────────────────────────

    /// <summary>
    /// Loads the event tree from the connected Wwise project.
    /// Called by <see cref="Services.WindowService.ShowMainWindow"/>
    /// after the window is visible — not from code-behind.
    ///
    /// Uses async/await so the UI thread stays responsive while the
    /// WAAPI query runs on a background thread.
    /// </summary>
    public async void InitializeAsync()
    {
        await LoadEventTreeAsync();
    }

    /// <summary>
    /// Core loading logic — separated from <see cref="InitializeAsync"/>
    /// so it can be awaited in unit tests.
    /// </summary>
    internal async Task LoadEventTreeAsync()
    {
        try
        {
            StatusMessage = "Loading events...";
            IsConnected = true;

            var nodes = await _objectService.FetchEventHierarchyAsync();

            EventTreeNodes.Clear();
            foreach (var node in nodes)
            {
                EventTreeNodes.Add(MapToViewModel(node));
            }

            var eventCount = CountLeafNodes(nodes);
            StatusMessage = eventCount > 0
                ? $"Connected — {eventCount} event(s) loaded"
                : "Connected — no events found in project";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load events: {ex.Message}";
            IsConnected = false;
        }
    }

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

        // TODO: When an event is selected, load its RTPC and Switch
        //       dependencies via IWwiseObjectService and populate
        //       CurrentEventRTPCs / CurrentEventSwitches.

        
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
        // TODO: Inject an IWwiseSoundEngineService and call
        //       PostEvent(SelectedEventNode.Id) here.
    }

    /// <summary>
    /// Stops all currently playing sounds.
    /// </summary>
    [RelayCommand]
    private void StopEvent()
    {
        // TODO: Inject an IWwiseSoundEngineService and call
        //       StopAll() here.
    }

    // ── Mapping Helpers ─────────────────────────────────────────

    /// <summary>
    /// Recursively maps a <see cref="WwiseEventNode"/> data model to an
    /// <see cref="EventNodeViewModel"/> for tree display.
    /// </summary>
    private static EventNodeViewModel MapToViewModel(WwiseEventNode model)
    {
        var vm = new EventNodeViewModel
        {
            Id       = model.Id,
            Name     = model.Name,
            IsFolder = model.IsFolder
        };

        foreach (var child in model.Children)
        {
            vm.Children.Add(MapToViewModel(child));
        }

        return vm;
    }

    /// <summary>
    /// Counts the total number of leaf (non-folder) nodes across the hierarchy.
    /// Used for the status message after loading.
    /// </summary>
    private static int CountLeafNodes(System.Collections.Generic.IEnumerable<WwiseEventNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (!node.IsFolder)
                count++;
            count += CountLeafNodes(node.Children);
        }
        return count;
    }
}