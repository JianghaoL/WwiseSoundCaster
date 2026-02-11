using System.Collections.Generic;

namespace WwiseSoundCaster.Models;

/// <summary>
/// Pure data model representing a node in the Wwise event hierarchy.
/// May be a folder (container / work unit) or a leaf event object.
///
/// This class is intentionally a plain POCO — it carries no UI or
/// MVVM concerns. The ViewModel layer (<see cref="ViewModels.EventNodeViewModel"/>)
/// maps these into observable tree nodes for the TreeView.
///
/// Architectural note:
///   Services return WwiseEventNode trees; ViewModels consume them.
///   This keeps the service layer free from Avalonia / UI dependencies.
/// </summary>
public class WwiseEventNode
{
    /// <summary>Wwise object GUID (empty for virtual folder nodes).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the node (folder or event name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Wwise object type (e.g. "Event", "Folder", "WorkUnit").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <c>true</c> when this node is a structural container (folder / work unit),
    /// <c>false</c> when it is a leaf event.
    /// </summary>
    public bool IsFolder { get; set; }

    /// <summary>Child nodes forming the subtree beneath this node.</summary>
    public List<WwiseEventNode> Children { get; set; } = new();
}
