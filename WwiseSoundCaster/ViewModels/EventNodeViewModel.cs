using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WwiseSoundCaster.Models;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Represents a node in the event tree hierarchy (folder, container, or object).
/// Used for the left-panel TreeView navigation.
///
/// Node type semantics:
///   • <see cref="WwiseNodeType.Folder"/> — expandable, does NOT populate right panel.
///   • <see cref="WwiseNodeType.Container"/> — NOT expandable, selectable (future container logic).
///   • <see cref="WwiseNodeType.Object"/> — leaf Sound, populates right panel.
/// </summary>
public partial class EventNodeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _id = string.Empty;

    /// <summary>
    /// Semantic classification of this node.
    /// Determines expansion behaviour, icon, and right-panel interaction.
    /// </summary>
    [ObservableProperty]
    private WwiseNodeType _nodeType;

    /// <summary>Notify computed properties when <see cref="NodeType"/> changes.</summary>
    partial void OnNodeTypeChanged(WwiseNodeType value)
    {
        OnPropertyChanged(nameof(IsFolder));
        OnPropertyChanged(nameof(IsContainer));
        OnPropertyChanged(nameof(IsObject));
        OnPropertyChanged(nameof(TreeChildren));
    }

    /// <summary>True when this node is a true organisational folder.</summary>
    public bool IsFolder => NodeType == WwiseNodeType.Folder;

    /// <summary>True when this node is an audio container (ActorMixer, SwitchContainer, etc.).</summary>
    public bool IsContainer => NodeType == WwiseNodeType.Container;

    /// <summary>True when this node is a playable leaf object (Sound).</summary>
    public bool IsObject => NodeType == WwiseNodeType.Object;

    /// <summary>
    /// Children exposed to the TreeView for expansion.
    /// Returns the real <see cref="Children"/> collection for Folders,
    /// and an empty collection for Containers and Objects — so only
    /// true folders are expandable in the UI.
    /// </summary>
    public ObservableCollection<EventNodeViewModel> TreeChildren =>
        NodeType == WwiseNodeType.Folder ? Children : EmptyChildren;

    /// <summary>
    /// Full child collection — always populated to preserve the
    /// complete Wwise hierarchy. Used by future container logic.
    /// </summary>
    public ObservableCollection<EventNodeViewModel> Children { get; } = new();

    /// <summary>
    /// Reference to the original WwiseEventNode data model.
    /// Used to lookup associated data in WwiseObjectHandler.nodeToObjectMap.
    /// </summary>
    public WwiseEventNode? SourceNode { get; set; }

    // ── Static helpers ──────────────────────────────────────────

    /// <summary>Shared empty collection so non-folder nodes never allocate.</summary>
    private static readonly ObservableCollection<EventNodeViewModel> EmptyChildren = new();
}
