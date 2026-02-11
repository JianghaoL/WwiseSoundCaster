using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WwiseSoundCaster.Models;

namespace WwiseSoundCaster.ViewModels;

/// <summary>
/// Represents a node in the event tree hierarchy (folder or event).
/// Used for the left-panel TreeView navigation.
/// </summary>
public partial class EventNodeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _id = string.Empty;

    /// <summary>
    /// True if this node represents a folder/category, false if it is a leaf event.
    /// </summary>
    [ObservableProperty]
    private bool _isFolder;

    /// <summary>
    /// Child nodes for hierarchical display.
    /// </summary>
    public ObservableCollection<EventNodeViewModel> Children { get; } = new();

    /// <summary>
    /// Reference to the original WwiseEventNode data model.
    /// Used to lookup associated data in WwiseObjectHandler.nodeToObjectMap.
    /// </summary>
    public WwiseEventNode? SourceNode { get; set; }
}
