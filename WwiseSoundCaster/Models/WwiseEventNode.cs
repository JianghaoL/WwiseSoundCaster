using System.Collections.Generic;

namespace WwiseSoundCaster.Models;

/// <summary>
/// Pure data model representing a node in the Wwise event hierarchy.
/// May be a folder, a container, or a playable leaf object.
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

    /// <summary>Display name of the node (folder, container, or object name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Wwise object type string (e.g. "Sound", "Folder", "WorkUnit", "ActorMixer").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Semantic classification of this node.
    /// Drives tree expansion, icon selection, and right-panel behaviour.
    /// </summary>
    public WwiseNodeType NodeType { get; set; }

    /// <summary>
    /// <c>true</c> only when this node is a true organisational folder
    /// (Folder or WorkUnit). Derived from <see cref="NodeType"/>.
    /// </summary>
    public bool IsFolder => NodeType == WwiseNodeType.Folder;

    /// <summary>
    /// Child nodes forming the subtree beneath this node.
    /// Populated for all node types to preserve the full hierarchy,
    /// but only Folder children are exposed in the TreeView.
    /// </summary>
    public List<WwiseEventNode> Children { get; set; } = new();
}
