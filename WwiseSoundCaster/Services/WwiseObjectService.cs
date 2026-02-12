using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WwiseSoundCaster.Models;

namespace WwiseSoundCaster.Services;

/// <summary>
/// Production implementation of <see cref="IWwiseObjectService"/>.
///
/// Delegates the raw WAAPI query work to <see cref="WwiseObjectHandler"/>
/// and returns clean <see cref="WwiseEventNode"/> hierarchies.
///
/// The connection guard ensures callers get an empty result instead of
/// an exception when WAAPI is not available.
/// </summary>
public class WwiseObjectService : IWwiseObjectService
{
    private readonly IWwiseConnectionService _connectionService;

    public WwiseObjectService(IWwiseConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    #region IWwiseObjectService

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WwiseEventNode>> FetchEventHierarchyAsync()
    {
        // Guard: do not attempt queries when disconnected.
        if (!_connectionService.IsConnected)
        {
            return Array.Empty<WwiseEventNode>();
        }

        try
        {
            // TODO: Extend this to also fetch RTPC and Switch dependencies
            //       per event (see architecture doc — event dependency extraction).
            return await WwiseObjectHandler.FetchWwiseObjectsAsync();
        }
        catch (Exception)
        {
            // TODO: Replace with proper logging abstraction (e.g. ILogger).
            return Array.Empty<WwiseEventNode>();
        }
    }

    #endregion
}
