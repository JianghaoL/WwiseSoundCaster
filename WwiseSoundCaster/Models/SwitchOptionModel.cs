namespace WwiseSoundCaster.Models;

/// <summary>
/// Pure data model representing a single switch option (child) within a
/// Wwise Switch Group or State Group.
///
/// This is a plain POCO — it carries no UI, MVVM, or WAAPI concerns.
/// The ViewModel layer (<see cref="ViewModels.SwitchGroupViewModel"/>)
/// consumes these via its <c>AvailableSwitches</c> collection.
///
/// Fields are populated from the WAAPI query result:
///   result["return"] → each item yields { id, name, type }.
///
/// Later, when WAAPI integration for SetSwitch is added, the
/// <see cref="Id"/> property will be passed to <c>ak.soundengine.setSwitch</c>.
/// </summary>
public class SwitchOptionModel
{
    /// <summary>Wwise object GUID for this switch state.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name of the switch state (displayed in the ComboBox).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Wwise object type (e.g. "Sound", "RandomSequenceContainer", etc.).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Returns the display name — used as fallback for non-templated controls.
    /// </summary>
    public override string ToString() => Name;
}
