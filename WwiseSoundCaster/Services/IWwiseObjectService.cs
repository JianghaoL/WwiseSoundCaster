using System.Collections.Generic;
using System.Threading.Tasks;
using WwiseSoundCaster.Models;

namespace WwiseSoundCaster.Services;

/// <summary>
/// Abstracts retrieval of Wwise project objects (events, folders, etc.).
///
/// Implementations translate raw WAAPI query results into clean
/// <see cref="WwiseEventNode"/> hierarchies.  ViewModels call this
/// service and map the results to their own observable view-models.
/// </summary>
public interface IWwiseObjectService
{
    /// <summary>
    /// Fetches the full event hierarchy from the connected Wwise project.
    /// Returns the top-level nodes; each node may have nested children.
    /// </summary>
    Task<IReadOnlyList<WwiseEventNode>> FetchEventHierarchyAsync();
}
