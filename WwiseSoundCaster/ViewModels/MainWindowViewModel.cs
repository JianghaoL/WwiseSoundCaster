using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
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
    public static WwiseObject? SelectedObject;

    public static JObject? GameObject;

    // Listener object for event posting when no specific game object is selected.
    public static JObject ListenerObject = new JObject
    {
        {"gameObject", 0}, // Arbitrary ID for the listener object
        {"name", "Listener"}
    };

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
    /// Display name of the currently selected object (derived from SelectedEventNode).
    /// Bound to the header title in the right panel.
    /// </summary>
    public string SelectedObjectName => SelectedEventNode?.Name ?? string.Empty;

    /// <summary>
    /// Note / comment text for the currently selected object.
    /// Displayed below the object name in the header panel.
    /// </summary>
    [ObservableProperty]
    private string _selectedObjectNote = string.Empty;

    /// <summary>
    /// Collection of all Wwise Events that reference the currently selected object.
    /// Displayed as playable "block" cards in the header area.
    ///
    /// Architecture note:
    ///   Each <see cref="EventViewModel"/> owns its own PlayCommand.
    ///   This ViewModel does NOT couple to EventViewModel — it only
    ///   populates the collection. The actual play callback should be
    ///   wired via a delegate or mediator pattern when WAAPI integration
    ///   is implemented.
    /// </summary>
    public ObservableCollection<EventViewModel> RelatedEvents { get; } = new();

    // Notify derived properties when SelectedEventNode changes
    partial void OnSelectedEventNodeChanged(EventNodeViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedObjectName));
        OnPropertyChanged(nameof(HasSelectedEvent));

        // ── Folder selected: do NOT populate the right-side panel. ──
        // Folders are purely organisational — selecting one clears
        // any previously displayed object data.
        if (value == null || value.NodeType == WwiseNodeType.Folder)
        {
            SelectedObject = null;
            SelectedObjectNote = string.Empty;
            RelatedEvents.Clear();
            CurrentEventRTPCs.Clear();
            CurrentEventSwitches.Clear();
            return;
        }

        // ── Object or Container selected: populate right panel. ──
        // Containers are treated like Objects for now.
        // TODO: Implement container-specific right-panel logic
        //       (e.g. display contained sounds, randomisation weights,
        //       switch assignments) when container browsing is added.
        if (value.SourceNode != null)
        {
            // Immediately clear stale right-panel data so the UI never
            // shows data from the previous selection during loading.
            RelatedEvents.Clear();
            CurrentEventRTPCs.Clear();
            CurrentEventSwitches.Clear();

            WwiseObjectHandler.nodeToObjectMap.TryGetValue(value.SourceNode, out SelectedObject);

            SelectedObjectNote = SelectedObject?.Notes ?? string.Empty;

            // Load event dependencies asynchronously (fire-and-forget)
            _ = LoadSelectedEventDependenciesAsync();
        }
        else
        {
            SelectedObject = null;
            SelectedObjectNote = string.Empty;
            RelatedEvents.Clear();
            CurrentEventRTPCs.Clear();
            CurrentEventSwitches.Clear();
        }
    }

    /// <summary>
    /// Loads related Events, RTPC, and Switch dependencies for the currently
    /// selected object. Called asynchronously when the selection changes.
    /// </summary>
    private async Task LoadSelectedEventDependenciesAsync()
    {
        if (SelectedObject == null || WwiseClient.client == null || !WwiseClient.isConnected)
        {
            return;
        }

        try
        {
            Console.WriteLine($"[MainWindowViewModel] Selected Object Name: {SelectedObject.Name}");

            // Cleanup: Unregister previous game object if applicable
            if (GameObject != null)
                await WwiseClient.client.Call("ak.soundengine.unregisterGameObj", new { gameObject = GameObject["gameObject"] });



            // Query WAAPI for events that reference the selected object
            var args = new JObject
            {
                {"waql", $"$ from type Action where target.id = \"{SelectedObject.Id}\""}
            };

            var options = new JObject
            {
                {"return", new JArray("parent.id", "parent.name")}
            };

            var result = await WwiseClient.client.Call(
                ak.wwise.core.@object.get, args, options);
            SelectedObject.Events = result;
            // Console.WriteLine($"[MainWindowViewModel] Events for selected object: {result["return"]?.ToString()}");

            // Populate RelatedEvents from the query result
            RelatedEvents.Clear();
            var returnArray = result["return"] as JArray;
            if (returnArray != null)
            {
                foreach (var item in returnArray)
                {
                    var eventVm = new EventViewModel
                    {
                        Id   = item["parent.id"]?.ToString() ?? string.Empty,
                        Name = item["parent.name"]?.ToString() ?? "(unnamed)"
                    };
                    RelatedEvents.Add(eventVm);
                }
            }



            // Register a gameobject for event posting
            GameObject = new JObject
            {
                {"gameObject", Random.Shared.Next(1, 9999)}, // Random ID for testing
                {"name", SelectedObject.Name}
            };
            // Console.WriteLine($"[MainWindowViewModel] Registering game object with WAAPI: {GameObject["gameObject"]}");
            await WwiseClient.client.Call(ak.soundengine.registerGameObj, GameObject);
            



            // Register the listener object as well (for testing)
            await WwiseClient.client.Call(ak.soundengine.registerGameObj, ListenerObject);

            var regRelationArgs = new 
            {
                emitter = GameObject["gameObject"],
                listeners = new[] { ListenerObject["gameObject"] }
            };

            await WwiseClient.client.Call("ak.soundengine.setListeners", regRelationArgs);

            // Load RTPC and Switch dependencies
            await LoadRtpcDependenciesAsync();
            await LoadSwitchDependenciesAsync();
        
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindowViewModel] Failed to load event dependencies: {ex.Message}");
            SetStatus($"Failed to load dependencies: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads RTPC (Game Parameter) dependencies for the currently selected object.
    /// Queries WAAPI for all RTPCs and populates the CurrentEventRTPCs collection.
    /// </summary>
    private async Task LoadRtpcDependenciesAsync()
    {
        if (SelectedObject == null || WwiseClient.client == null || !WwiseClient.isConnected)
        {
            return;
        }

        try
        {
            // Query WAAPI for all Game Parameters (RTPCs)
            var args = new JObject
            {
                //{"waql", $"$ \"{SelectedObject.Id}\""}

                { "waql", $"$ from object \"{SelectedObject.Id}\" select @RTPC" }

                //{ "waql", $"$ from object \"{SelectedObject.Id}\" select descendants where type = \"RTPC\"" }
            };

            var options = new JObject
            {
                //{"return", new JArray("id", "name", "@Min", "@Max", "@BindToBuiltInParam")}

                //{"return", new JArray("@RTPC")}

                { "return", new JArray { "id", "@PropertyName", "@ControlInput.name" } }
            };


            var result = await WwiseClient.client.Call(
                ak.wwise.core.@object.get, args, options);

            if (result["return"] == null || result["return"]?.Count() == 0)
            {
                CurrentEventRTPCs.Clear();
                return;
            }

            //Console.WriteLine($"[MainWindowViewModel] RTPCs for selected object: {result["return"]?.ToString()}");

            // Populate CurrentEventRTPCs from the query result
            CurrentEventRTPCs.Clear();
            var returnArray = result["return"] as JArray;
            if (returnArray != null)
            {
                foreach (var item in returnArray)
                {


                    var rtpcVm = new RtpcViewModel
                    {
                        Id                  = item["id"]?.ToString() ?? string.Empty,
                        Name                = item["@ControlInput.name"]?.ToString() ?? "(unnamed)",
                        PropertyDisplayName = item["@PropertyName"]?.ToString() ?? string.Empty,
                        // MinValue            = item["@Min"]?.Value<double>() ?? 0.0,
                        // MaxValue            = item["@Max"]?.Value<double>() ?? 100.0,
                        // CurrentValue        = item["@Min"]?.Value<double>() ?? 0.0 // Start at min value
                    };
                    CurrentEventRTPCs.Add(rtpcVm);
                    await rtpcVm.SetRangeAsync(item as JObject);
                }
            }

            //Console.WriteLine($"[MainWindowViewModel] Loaded {CurrentEventRTPCs.Count} RTPC(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindowViewModel] Failed to load RTPC dependencies: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads Switch Group dependencies for the currently selected object.
    /// Queries WAAPI for all Switch Groups and their states, then populates
    /// the CurrentEventSwitches collection.
    ///
    /// Switch resolution strategy:
    ///   • If the selected object IS a SwitchContainer, query its own configuration.
    ///   • Otherwise, search ancestors for a SwitchContainer parent.
    /// </summary>
    private async Task LoadSwitchDependenciesAsync()
    {
        if (SelectedObject == null || WwiseClient.client == null || !WwiseClient.isConnected)
        {
            return;
        }

        try
        {
            // Step 1: Resolve the SwitchContainer and its properties.
            // Different query depending on whether the selection IS the container.
            JObject? switchContainerData = await ResolveSwitchContainerAsync();

            if (switchContainerData == null)
            {
                CurrentEventSwitches.Clear();
                return;
            }

            // Step 2: Extract the Switch Group/State Group name and container ID
            var switchGroupName = switchContainerData["@SwitchGroupOrStateGroup.name"]?.ToString()
                                  ?? switchContainerData["name"]?.ToString()
                                  ?? "Switch Group";
            var switchContainerId = switchContainerData["id"]?.ToString() ?? string.Empty;

            Console.WriteLine($"[MainWindowViewModel] Resolved SwitchContainer: {switchGroupName}");

            // Step 3: Query the children of the Switch Group / State Group
            var stateOptions = await FetchSwitchStatesAsync(switchContainerId);

            // Step 4: Populate the ViewModel
            CurrentEventSwitches.Clear();

            if (stateOptions != null && stateOptions.Count > 0)
            {
                var switchGroupVm = new SwitchGroupViewModel
                {
                    Id        = switchContainerId,
                    GroupName = switchGroupName
                };

                foreach (var item in stateOptions)
                {
                    var option = new SwitchOptionModel
                    {
                        Id   = item["id"]?.ToString()   ?? string.Empty,
                        Name = item["name"]?.ToString() ?? "(unnamed)",
                        Type = item["type"]?.ToString() ?? string.Empty
                    };
                    switchGroupVm.AvailableSwitches.Add(option);
                }

                // Auto-select the first option so the ComboBox is never empty
                if (switchGroupVm.AvailableSwitches.Count > 0)
                {
                    switchGroupVm.SelectedSwitch = switchGroupVm.AvailableSwitches[0];
                }

                CurrentEventSwitches.Add(switchGroupVm);
            }

            Console.WriteLine($"[MainWindowViewModel] Loaded {CurrentEventSwitches.Count} Switch Group(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindowViewModel] Failed to load Switch dependencies: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolves the SwitchContainer for the currently selected object.
    /// Returns the container's data (id, name, @SwitchGroupOrStateGroup.name).
    ///
    /// Resolution logic:
    ///   • If SelectedObject.Type == "SwitchContainer": query itself.
    ///   • Otherwise: query ancestors for a SwitchContainer parent.
    /// </summary>
    private async Task<JObject?> ResolveSwitchContainerAsync()
    {
        if (SelectedObject == null || WwiseClient.client == null)
            return null;

        JObject args;
        JObject options = new JObject
        {
            {"return", new JArray("name", "@SwitchGroupOrStateGroup.name", "id")}
        };

        // Case 1: The selected object IS a SwitchContainer
        if (SelectedObject.Type == "SwitchContainer")
        {
            args = new JObject
            {
                {"waql", $"$ from object \"{SelectedObject.Id}\""}
            };
        }
        // Case 2: The selected object is a child — find its SwitchContainer ancestor
        else
        {
            args = new JObject
            {
                {"waql", $"$ from object \"{SelectedObject.Id}\" select ancestors where type = \"SwitchContainer\""}
            };
        }

        var result = await WwiseClient.client.Call(
            ak.wwise.core.@object.get, args, options);

        var returnArray = result["return"] as JArray;
        return (returnArray != null && returnArray.Count > 0)
            ? returnArray[0] as JObject
            : null;
    }

    /// <summary>
    /// Fetches the available switch states (children) for a given SwitchContainer.
    /// Queries the @SwitchGroupOrStateGroup and retrieves its child states.
    /// </summary>
    private async Task<JArray?> FetchSwitchStatesAsync(string switchContainerId)
    {
        if (string.IsNullOrEmpty(switchContainerId) || WwiseClient.client == null)
            return null;

        var args = new JObject
        {
            {"waql", $"$ \"{switchContainerId}\" select @SwitchGroupOrStateGroup select children"}
        };

        var options = new JObject
        {
            {"return", new JArray("id", "name", "type")}
        };

        var result = await WwiseClient.client.Call(
            ak.wwise.core.@object.get, args, options);

        Console.WriteLine($"[MainWindowViewModel] Switch states for container: {result["return"]?.ToString()}");

        return result["return"] as JArray;
    }

    /// <summary>
    /// True when a non-folder node is selected — used to toggle right-panel visibility.
    /// Folders are organisational only and do NOT show the right-side control panel.
    /// </summary>
    public bool HasSelectedEvent =>
        SelectedEventNode is not null
        && SelectedEventNode.NodeType != WwiseNodeType.Folder;

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

    // ── Public Methods ──────────────────────────────────────────

    /// <summary>
    /// Updates the bottom status message in a thread-safe manner.
    /// Safe to call from any thread — dispatches to the UI thread
    /// when necessary.
    /// </summary>
    /// <param name="message">The status text to display.</param>
    public void SetStatus(string message)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            StatusMessage = message;
        }
        else
        {
            Dispatcher.UIThread.Post(() => StatusMessage = message);
        }
    }

    // ── Mapping Helpers ─────────────────────────────────────────

    /// <summary>
    /// Recursively maps a <see cref="WwiseEventNode"/> data model to an
    /// <see cref="EventNodeViewModel"/> for tree display.
    ///
    /// All children are mapped regardless of node type so that the
    /// full hierarchy is preserved in the ViewModel. The TreeView
    /// only expands Folder nodes via <see cref="EventNodeViewModel.TreeChildren"/>.
    /// </summary>
    private static EventNodeViewModel MapToViewModel(WwiseEventNode model)
    {
        var vm = new EventNodeViewModel
        {
            Id         = model.Id,
            Name       = model.Name,
            NodeType   = model.NodeType,
            SourceNode = model  // Store reference to original model
        };

        foreach (var child in model.Children)
        {
            vm.Children.Add(MapToViewModel(child));
        }

        return vm;
    }

    /// <summary>
    /// Counts the total number of leaf Object nodes (Sounds) across the hierarchy.
    /// Used for the status message after loading.
    /// </summary>
    private static int CountLeafNodes(System.Collections.Generic.IEnumerable<WwiseEventNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node.NodeType == WwiseNodeType.Object)
                count++;
            count += CountLeafNodes(node.Children);
        }
        return count;
    }
}