using System.Threading.Tasks;

namespace WwiseSoundCaster.Services;

/// <summary>
/// Production implementation of <see cref="IWwiseConnectionService"/>.
///
/// Wraps the static <see cref="WwiseClient"/> so that ViewModels never
/// reference it directly. All WAAPI transport concerns are contained here.
///
/// Architectural note:
///   When the WAAPI client library is replaced or upgraded, only this
///   class (and <see cref="WwiseObjectService"/>) need to change —
///   ViewModels and Views remain untouched.
/// </summary>
public class WwiseConnectionService : IWwiseConnectionService
{
    #region IWwiseConnectionService

    /// <inheritdoc/>
    public bool IsConnected => WwiseClient.isConnected;

    /// <inheritdoc/>
    public string? StatusMessage => WwiseClient.message;

    /// <inheritdoc/>
    public async Task ConnectAsync()
    {
        // TODO: Accept configurable URI (e.g. ws://localhost:8080/waapi) and timeout
        //       via constructor parameters or method overload.
        // TODO: After successful connection, register a default game object via
        //       ak.soundengine.registerGameObj for playback operations.
        await WwiseClient.Connect();
    }

    /// <inheritdoc/>
    public Task DisconnectAsync()
    {
        // TODO: Implement graceful WAAPI disconnect:
        //       - Unregister game objects
        //       - Dispose the JsonClient
        //       - Reset WwiseClient.isConnected
        return Task.CompletedTask;
    }

    #endregion
}
