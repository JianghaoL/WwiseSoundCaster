using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WwiseSoundCaster.Services;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// ViewModel for the startup Wwise connection window.
///
/// Provides a command to attempt WAAPI connection and exposes
/// status text / colour state for the UI to bind against.
///
/// Architectural notes:
///   • This ViewModel has <b>no knowledge</b> of <c>MainWindow</c> or
///     <c>MainWindowViewModel</c>. Navigation is delegated entirely to
///     <see cref="IWindowService"/>.
///   • The WAAPI transport is abstracted behind
///     <see cref="IWwiseConnectionService"/> — no direct reference to
///     <c>WwiseClient</c>.
///   • Both dependencies are constructor-injected, making this class
///     fully testable with mocks.
/// </summary>
public partial class WwiseConnectWindowViewModel : ViewModelBase
{
    // ── Dependencies ────────────────────────────────────────────

    private readonly IWwiseConnectionService _connectionService;
    private readonly IWindowService _windowService;

    // ── Constructor ─────────────────────────────────────────────

    public WwiseConnectWindowViewModel(
        IWwiseConnectionService connectionService,
        IWindowService windowService)
    {
        _connectionService = connectionService;
        _windowService = windowService;
    }

    // ── Connection Status ───────────────────────────────────────

    /// <summary>
    /// Human-readable status message shown beneath the connect button.
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
    ///
    /// On success:
    ///   1. Sets status to Connected (green).
    ///   2. Briefly pauses so the user sees the success state.
    ///   3. Opens the MainWindow via <see cref="IWindowService"/>.
    ///   4. Closes this connect window.
    ///
    /// On failure:
    ///   Sets status to Failed (red) with an error message.
    /// </summary>
    [RelayCommand]
    private async Task ConnectToWwiseAsync()
    {
        StatusMessage = "Connecting...";
        Status = ConnectionStatus.None;

        // ─────────────────────────────────────────────────────────
        // TODO: Future enhancements:
        //   • Accept a user-specified URI from a TextBox binding.
        //   • Add a CancellationToken so the user can abort a slow attempt.
        //   • After connecting, call RegisterGameObjectAsync() on the
        //     connection service for playback support.
        // ─────────────────────────────────────────────────────────

        await _connectionService.ConnectAsync();

        if (_connectionService.IsConnected)
        {
            // ── Success path ────────────────────────────────────
            Status = ConnectionStatus.Connected;
            StatusMessage = _connectionService.StatusMessage
                            ?? "Connected to Wwise";

            // Brief delay so the user can visually confirm the green state.
            await Task.Delay(600);

            // Navigate to the main window and close this one.
            // No direct reference to MainWindow or its ViewModel — fully
            // decoupled through IWindowService.
            _windowService.ShowMainWindow();
            _windowService.CloseWindow(this);
        }
        else
        {
            // ── Failure path ────────────────────────────────────
            Status = ConnectionStatus.Failed;
            StatusMessage = _connectionService.StatusMessage
                            ?? "Connection failed — is Wwise running with WAAPI enabled?";
        }
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
