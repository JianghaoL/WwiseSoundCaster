using System.Threading.Tasks;

namespace WwiseSoundCaster.Services;

/// <summary>
/// Abstracts the Wwise WAAPI connection lifecycle.
///
/// Implementations wrap the underlying transport (e.g. <c>WwiseClient</c>),
/// keeping ViewModels completely decoupled from concrete WAAPI details.
/// This enables unit-testing ViewModels with a mock connection service.
/// </summary>
public interface IWwiseConnectionService
{
    /// <summary>Whether a WAAPI session is currently active.</summary>
    bool IsConnected { get; }

    /// <summary>Human-readable message from the last connection attempt.</summary>
    string? StatusMessage { get; }

    /// <summary>
    /// Attempt to establish a WAAPI connection to the running Wwise instance.
    /// On success <see cref="IsConnected"/> becomes <c>true</c>.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Gracefully tear down the WAAPI session.
    /// </summary>
    Task DisconnectAsync();
}
