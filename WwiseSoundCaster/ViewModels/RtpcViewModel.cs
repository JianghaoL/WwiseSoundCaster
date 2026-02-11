using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Represents a single RTPC (Game Parameter) control.
///
/// Architecture:
///   • The Slider and inline TextBox both bind TwoWay to <see cref="CurrentValue"/>.
///   • Value validation (clamping to Min/Max) occurs in <see cref="OnCurrentValueChanged"/>.
///   • Editing mode (<see cref="IsEditing"/>) is a ViewModel-driven state —
///     the View switches between TextBlock and TextBox via IsVisible bindings.
///   • This ViewModel has no dependency on MainWindowViewModel or WwiseClient.
/// </summary>
public partial class RtpcViewModel : ViewModelBase
{
    public JObject? RTPC;

    // ── Display Properties ──────────────────────────────────────

    /// <summary>
    /// Descriptive label for the property this RTPC controls
    /// (e.g., "Volume", "Pitch"). Displayed above the RTPC name.
    /// Populated from WAAPI @PropertyName.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPropertyDisplayName))]
    private string _propertyDisplayName = string.Empty;

    /// <summary>
    /// True when <see cref="PropertyDisplayName"/> has content — used to
    /// conditionally show the property label row in the UI.
    /// </summary>
    public bool HasPropertyDisplayName => !string.IsNullOrEmpty(PropertyDisplayName);

    /// <summary>
    /// Name of the RTPC parameter (e.g., the Game Parameter name).
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Wwise object ID for the RTPC parameter.
    /// </summary>
    [ObservableProperty]
    private string _id = string.Empty;

    // ── Value Range ─────────────────────────────────────────────

    [ObservableProperty]
    private double _minValue;

    [ObservableProperty]
    private double _maxValue = 100.0;

    public async Task SetRangeAsync(JObject rtpc)
    {
        if (rtpc == null) return;
        if (WwiseClient.client == null) return;


        RTPC = rtpc;

        var args = new JObject
        {
            {"waql", $"$\"{rtpc["id"]}\""},
        };

        var options = new JObject
        {
            {"return", new JArray("@Curve")}
        };

        var result = await WwiseClient.client?.Call(ak.wwise.core.@object.get, args, options);
        Console.WriteLine($"[RtpcViewModel] SetRangeAsync result: {result["return"]?.ToString()}");
        
        // Access the points array: result["return"] is a JArray, so use [0] (int index) not ["0"] (string key)
        var points = result?["return"]?[0]?["@Curve"]?["points"] as JArray;

        double min = double.MaxValue;
        double max = double.MinValue;

        if (points != null)
        {
            foreach (var point in points)
            {
                var xValue = point["x"]?.ToObject<double>() ?? 0;
                if (xValue < min) min = xValue;
                if (xValue > max) max = xValue;
            }
        }

        // If no valid points found, use defaults
        if (min == double.MaxValue || max == double.MinValue)
        {
            min = 0;
            max = 100;
        }

        MinValue = min;
        MaxValue = max;

        CurrentValue = min; // Initialize to min or some default value within the range
    }


    // ── Current Value ───────────────────────────────────────────

    /// <summary>
    /// Current value of the RTPC parameter. Bound TwoWay to both
    /// the Slider and the numeric display.
    ///
    /// Validation: Clamped to [MinValue, MaxValue] range.
    /// WAAPI integration: SetRTPCValue should be triggered here.
    /// </summary>
    [ObservableProperty]
    private double _currentValue;

    /// <summary>
    /// Called automatically when <see cref="CurrentValue"/> changes.
    /// Performs value clamping and is the designated hook point for
    /// future WAAPI SetRTPCValue calls.
    /// </summary>
    partial void OnCurrentValueChanged(double value)
    {
        // ── Value Validation ──
        // Clamp the value within the allowed [MinValue, MaxValue] range.
        // If the incoming value is out of range, re-set to the clamped value.
        var clamped = Math.Clamp(value, MinValue, MaxValue);
        if (Math.Abs(clamped - value) > double.Epsilon)
        {
            CurrentValue = clamped;
            return; // OnCurrentValueChanged will be re-invoked with the clamped value
        }

        // ── WAAPI Integration Point ──
        // TODO: Trigger WAAPI SetRTPCValue call here.
        //       Example: await _wwiseService.SetRTPCValueAsync(Id, clamped, gameObjectId);
        //       The service call should be fire-and-forget or debounced to avoid
        //       flooding WAAPI during rapid slider movement.
    }

    // ── Inline Editing State ────────────────────────────────────

    /// <summary>
    /// When true, the UI shows an editable TextBox instead of the
    /// read-only TextBlock for the current value display.
    /// Toggled by <see cref="BeginEditCommand"/>, <see cref="CommitEditCommand"/>,
    /// and <see cref="CancelEditCommand"/>.
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// Temporary text buffer used during inline editing.
    /// Bound TwoWay to the editing TextBox. Parsed and validated
    /// on commit.
    /// </summary>
    [ObservableProperty]
    private string _editText = "0";

    // ── Editing Commands ────────────────────────────────────────

    /// <summary>
    /// Enters editing mode. Triggered by double-clicking the value display.
    /// Populates <see cref="EditText"/> with the current formatted value.
    /// </summary>
    [RelayCommand]
    private void BeginEdit()
    {
        EditText = CurrentValue.ToString("F1");
        IsEditing = true;
    }

    /// <summary>
    /// Commits the edited value. Triggered by:
    ///   • Pressing Enter (via KeyBinding)
    ///   • TextBox losing focus (via TextBoxCommitBehavior)
    ///
    /// Validation:
    ///   • Parses the text as a double.
    ///   • Clamps to [MinValue, MaxValue] range.
    ///   • If parsing fails, the edit is silently discarded.
    /// </summary>
    [RelayCommand]
    private void CommitEdit()
    {
        // Guard against double-commit (e.g., Enter key followed by LostFocus)
        if (!IsEditing) return;

        // ── Input Validation ──
        // Safely parse the user's text input. Invalid input is discarded.
        if (double.TryParse(EditText, out var parsed))
        {
            // Clamp within allowed range before assigning.
            // OnCurrentValueChanged will handle further validation and WAAPI trigger.
            CurrentValue = Math.Clamp(parsed, MinValue, MaxValue);
        }

        IsEditing = false;
    }

    /// <summary>
    /// Cancels editing without committing. Triggered by pressing Escape.
    /// The original <see cref="CurrentValue"/> is preserved.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        // Simply exit editing mode; the original CurrentValue remains unchanged.
        IsEditing = false;
    }
}
