namespace WwiseSoundCaster.Models;

/// <summary>
/// Classifies a node in the Wwise object hierarchy.
///
/// Used by <see cref="WwiseEventNode"/> and the ViewModel layer to
/// drive tree expansion, icon selection, and right-panel behaviour
/// without embedding Wwise-specific type strings in the UI.
///
/// Architectural note:
///   The enum lives in the <c>Models</c> layer so that both the
///   hierarchy builder (<see cref="WwiseObjectHandler"/>) and the
///   ViewModel layer can reference it without cross-layer coupling.
/// </summary>
public enum WwiseNodeType
{
    /// <summary>
    /// A true organisational folder (Wwise Folder or WorkUnit).
    /// Expandable in the TreeView; does NOT populate the right panel.
    /// </summary>
    Folder,

    /// <summary>
    /// An audio container (ActorMixer, RandomSequenceContainer,
    /// SwitchContainer, BlendContainer, etc.).
    /// Appears in the tree but is NOT expandable.
    /// Container-specific browsing logic will be added in the future.
    /// </summary>
    Container,

    /// <summary>
    /// A playable leaf object (typically a Sound).
    /// Selecting it populates the right-side control panel.
    /// </summary>
    Object
}
