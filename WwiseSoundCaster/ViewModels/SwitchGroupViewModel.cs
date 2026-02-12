using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using WwiseSoundCaster.Models;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Self-contained ViewModel for a single Switch Group control.
///
/// Architecture:
///   • The ComboBox binds its <c>ItemsSource</c> to <see cref="AvailableSwitches"/>
///     and its <c>SelectedItem</c> TwoWay to <see cref="SelectedSwitch"/>.
///   • This class is fully decoupled from <see cref="MainWindowViewModel"/> and
///     from any WAAPI client — it only exposes data for the View.
///   • Call <see cref="GetSelectedSwitchId"/> to retrieve the selected switch's
///     Wwise object GUID. This ID will later be passed to
///     <c>ak.soundengine.setSwitch</c> in the service layer.
///   • The old <c>AvailableStates</c> / <c>SelectedState</c> string-based API
///     is replaced by richer <see cref="SwitchOptionModel"/> objects so that
///     the Id and Type travel with each option without extra lookups.
///
/// Extensibility:
///   Because each group is its own ViewModel instance and the parent only holds
///   an <c>ObservableCollection&lt;SwitchGroupViewModel&gt;</c>, adding support
///   for multiple Switch Groups requires zero structural changes — just add
///   more instances to the collection.
/// </summary>
public partial class SwitchGroupViewModel : ViewModelBase
{
    // ── Identity ────────────────────────────────────────────────

    /// <summary>Wwise object GUID of the Switch Group itself.</summary>
    [ObservableProperty]
    private string _id = string.Empty;

    /// <summary>Human-readable name shown as the section header in the UI.</summary>
    [ObservableProperty]
    private string _groupName = string.Empty;

    // ── Switch Options ──────────────────────────────────────────

    /// <summary>
    /// All available switch options (children) within this group.
    /// Populated from WAAPI result parsing in the service / loader layer.
    /// The ComboBox <c>ItemsSource</c> binds here.
    /// </summary>
    public ObservableCollection<SwitchOptionModel> AvailableSwitches { get; } = new();

    /// <summary>
    /// Currently selected switch option.
    /// The ComboBox <c>SelectedItem</c> binds here (TwoWay).
    /// When WAAPI integration is added, subscribe to changes on this
    /// property to trigger <c>ak.soundengine.setSwitch</c>.
    /// </summary>
    [ObservableProperty]
    private SwitchOptionModel? _selectedSwitch;

    // ── Selection Changed Callback ──────────────────────────────

    /// <summary>
    /// Called automatically by CommunityToolkit.Mvvm every time
    /// <see cref="SelectedSwitch"/> changes (i.e. the user picks a
    /// new item in the ComboBox).
    ///
    /// This is the hook point for future WAAPI integration:
    ///   await wwiseService.SetSwitch(Id, newValue.Id);
    ///
    /// No direct WAAPI call should live here — delegate to a service
    /// via an <c>Action</c> / event / mediator injected at construction.
    /// </summary>
    partial void OnSelectedSwitchChanged(SwitchOptionModel? value)
    {
        if (value == null) return;

        // Trigger async WAAPI call in fire-and-forget manner
        _ = SendSwitchChangeToWwiseAsync(value);
    }

    /// <summary>
    /// Sends the switch state change to Wwise via WAAPI.
    /// Called asynchronously when SelectedSwitch changes.
    /// </summary>
    private async Task SendSwitchChangeToWwiseAsync(SwitchOptionModel selectedSwitch)
    {
        if (WwiseClient.client == null || !WwiseClient.isConnected)
        {
            return;
        }

        try
        {
            var args = new JObject
            {
                {"switchGroup", GroupName},
                {"switchState", selectedSwitch.Name},
                {"gameObject", MainWindowViewModel.GameObject?["gameObject"]}
            };

            var result = await WwiseClient.client.Call(ak.soundengine.setSwitch, args);
        }
        catch (Exception)
        {
            // Switch change failed - silently ignore to avoid disrupting user interaction
        }
    }

    // ── Public API ──────────────────────────────────────────────

    /// <summary>
    /// Returns the Wwise object GUID of the currently selected switch,
    /// or <c>null</c> if nothing is selected.
    ///
    /// Usage (future WAAPI integration in the service layer):
    /// <code>
    /// var switchId = switchGroupVm.GetSelectedSwitchId();
    /// if (switchId != null)
    ///     await wwiseService.SetSwitch(switchGroupVm.Id, switchId);
    /// </code>
    /// </summary>
    public string? GetSelectedSwitchId() => SelectedSwitch?.Id;

    /// <summary>
    /// Returns the Wwise object name of the currently selected switch,
    /// or <c>null</c> if nothing is selected.
    /// </summary>
    public string? GetSelectedSwitchName() => SelectedSwitch?.Name;
}
