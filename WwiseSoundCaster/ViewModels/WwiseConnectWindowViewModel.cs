using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// ViewModel for the startup Wwise connection window.
/// Provides a command to attempt WAAPI connection and exposes
/// status text / colour state for the UI to bind against.
/// </summary>
public partial class WwiseConnectWindowViewModel : ViewModelBase
{
    // ── Connection Status ───────────────────────────────────────

    /// <summary>
    /// Human-readable status message shown beneath the connect button.
    /// Possible values: "Not connected", "Connected", "Connection failed".
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Not connected";

    /// <summary>
    /// Semantic status used by the converter to pick the correct colour.
    /// <list type="bullet">
    ///   <item><description><see cref="ConnectionStatus.None"/>     → Gray</description></item>
    ///   <item><description><see cref="ConnectionStatus.Connected"/> → Green</description></item>
    ///   <item><description><see cref="ConnectionStatus.Failed"/>   → Red</description></item>
    /// </list>
    /// </summary>
    [ObservableProperty]
    private ConnectionStatus _status = ConnectionStatus.None;

    // ── Commands ────────────────────────────────────────────────

    /// <summary>
    /// Attempts to connect to the Wwise Authoring API (WAAPI).
    /// Bound to the "Connect" button in the view.
    /// </summary>
    [RelayCommand]
    private async Task ConnectToWwiseAsync()
    {
        StatusMessage = "Connecting...";
        Status = ConnectionStatus.None;

        // TODO: Replace the block below with real WAAPI connection logic.
        // ─────────────────────────────────────────────────────────
        // Example future implementation:
        //
        //   try
        //   {
        //       await _wwiseService.ConnectAsync("ws://localhost:8080/waapi");
        //       await _wwiseService.RegisterGameObjectAsync();
        //       Status        = ConnectionStatus.Connected;
        //       StatusMessage = "Connected to Wwise";
        //   }
        //   catch (Exception ex)
        //   {
        //       Status        = ConnectionStatus.Failed;
        //       StatusMessage = $"Connection failed: {ex.Message}";
        //   }
        // ─────────────────────────────────────────────────────────

        // Simulated delay so the user can see the "Connecting..." state
        await Task.Delay(500);

        // Placeholder result — default to failure until real logic is wired
        Status = ConnectionStatus.Failed;
        StatusMessage = "Connection failed — WAAPI logic not implemented yet";
    }
}

/// <summary>
/// Represents the tri-state result of a Wwise connection attempt.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>No connection attempt has been made (or in progress).</summary>
    None,

    /// <summary>Successfully connected to Wwise WAAPI.</summary>
    Connected,

    /// <summary>Connection attempt failed.</summary>
    Failed
}
