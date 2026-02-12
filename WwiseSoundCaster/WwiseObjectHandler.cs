using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WwiseSoundCaster.Models;

/// <summary>
/// Internal handler that performs raw WAAPI queries and transforms the
/// JSON results into <see cref="WwiseEventNode"/> hierarchies.
///
/// This class is <b>not</b> exposed to ViewModels directly — it is
/// wrapped by <see cref="WwiseSoundCaster.Services.WwiseObjectService"/>.
///
/// Keeping it static and internal preserves backward compatibility
/// while the service layer owns the public contract.
/// </summary>
public static class WwiseObjectHandler
{
    public static Dictionary<WwiseEventNode, WwiseObject> nodeToObjectMap = new Dictionary<WwiseEventNode, WwiseObject>();

    // ── Wwise Type Classification ───────────────────────────────

    /// <summary>
    /// Known Wwise types that represent true organisational folders.
    /// Only these types produce expandable tree nodes.
    /// </summary>
    private static readonly HashSet<string> FolderTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Folder", "WorkUnit"
    };

    /// <summary>
    /// Known Wwise types that represent audio containers.
    /// These appear in the tree but are NOT expandable.
    /// Container-specific browsing logic will be added in the future.
    /// </summary>
    private static readonly HashSet<string> ContainerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ActorMixer", "RandomSequenceContainer", "SwitchContainer", "BlendContainer"
    };

    // ── Public API ──────────────────────────────────────────────

    /// <summary>
    /// Queries the connected Wwise project for all Sound objects
    /// and builds a hierarchical tree with proper node type classification.
    /// </summary>
    /// <returns>
    /// Top-level <see cref="WwiseEventNode"/> list (folders, containers, and objects).
    /// Returns an empty list when the client is not connected or the query
    /// yields no results.
    /// </returns>
    public static async Task<IReadOnlyList<WwiseEventNode>> FetchWwiseObjectsAsync()
    {
        if (WwiseClient.client == null || !WwiseClient.isConnected)
        {
            return Array.Empty<WwiseEventNode>();
        }

        try
        {
            // Step 1: Fetch all Sound objects (leaf nodes).
            var soundArgs = new JObject
            {
                {"waql", "from type Sound"},
            };

            var soundOptions = new JObject
            {
                {"return", new JArray("id", "name", "notes", "type", "playbackDuration", "path", "extractEvents")}
            };

            var soundResult = await WwiseClient.client.Call(
                ak.wwise.core.@object.get, soundArgs, soundOptions);

            var soundObjects = soundResult["return"] as JArray;

            if (soundObjects == null || soundObjects.Count == 0)
            {
                return Array.Empty<WwiseEventNode>();
            }

            // Step 2: Fetch intermediate structural objects (folders, work units,
            //         containers) so we can classify each path segment correctly.
            var intermediateMap = await FetchIntermediateObjectsAsync();

            return BuildHierarchy(soundObjects, intermediateMap);
        }
        catch (Exception)
        {
            // TODO: Replace with proper logging when a logging framework is added.
            return Array.Empty<WwiseEventNode>();
        }
    }

    // ── Intermediate Type Resolution ────────────────────────────

    /// <summary>
    /// Queries WAAPI for all structural objects (folders, work units,
    /// and containers) and returns a map of normalised path → object data.
    /// Used by <see cref="BuildHierarchy"/> to classify intermediate nodes.
    /// </summary>
    private static async Task<Dictionary<string, JObject>> FetchIntermediateObjectsAsync()
    {
        var map = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);

        if (WwiseClient.client == null || !WwiseClient.isConnected)
            return map;

        try
        {
            var args = new JObject
            {
                {"waql", "from type WorkUnit, Folder, ActorMixer, RandomSequenceContainer, SwitchContainer, BlendContainer"}
            };

            var options = new JObject
            {
                {"return", new JArray("id", "name", "type", "path", "notes")}
            };

            var result = await WwiseClient.client.Call(
                ak.wwise.core.@object.get, args, options);

            var objects = result["return"] as JArray;

            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    var path = obj["path"]?.ToString();
                    if (!string.IsNullOrEmpty(path))
                    {
                        var normalised = NormalisePath(path);
                        map[normalised] = obj as JObject ?? new JObject();
                    }
                }
            }
        }
        catch (Exception)
        {
            // Non-fatal: BuildHierarchy will fall back to Folder for unknown types.
        }

        return map;
    }

    // ── Hierarchy Builder ───────────────────────────────────────

    /// <summary>
    /// Transforms a flat list of Sound objects (each carrying a <c>path</c> field)
    /// into a nested <see cref="WwiseEventNode"/> tree.
    ///
    /// Node type classification:
    ///   • <b>Folder</b> — true organisational nodes (Folder, WorkUnit).
    ///     Expandable in the TreeView, children populated.
    ///   • <b>Container</b> — audio containers (ActorMixer, RandomSequenceContainer, etc.).
    ///     Present in the tree but NOT expandable. Children are stored in the
    ///     data model for future container-specific logic.
    ///   • <b>Object</b> — playable leaf nodes (Sound).
    ///
    /// This method performs <b>pure data construction</b> — no UI logic.
    /// </summary>
    private static IReadOnlyList<WwiseEventNode> BuildHierarchy(
        JArray soundObjects,
        Dictionary<string, JObject> intermediateMap)
    {
        // Cache of already-created intermediate nodes keyed by their full
        // normalised path prefix so we never duplicate nodes.
        var nodeCache = new Dictionary<string, WwiseEventNode>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<WwiseEventNode>();

        foreach (var obj in soundObjects)
        {
            var fullPath = obj["path"]?.ToString() ?? string.Empty;
            var segments = fullPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            // A valid path has at least the root category and the Sound name.
            if (segments.Length < 2) continue;

            // Walk the intermediate segments (skip index 0 — the root category
            // e.g. "Actor-Mixer Hierarchy" — we do not show it as a node).
            WwiseEventNode? parent = null;

            for (int i = 1; i < segments.Length - 1; i++)
            {
                var key = string.Join('\\', segments, 0, i + 1);

                if (!nodeCache.TryGetValue(key, out var node))
                {
                    // Resolve the Wwise type from the intermediate query.
                    var intermediateInfo = intermediateMap.TryGetValue(key, out var infoObj) ? infoObj : null;
                    var wwiseType = intermediateInfo?["type"]?.ToString() ?? "Folder";
                    var nodeType  = ClassifyNodeType(wwiseType);
                    var nodeId    = intermediateInfo?["id"]?.ToString() ?? string.Empty;

                    node = new WwiseEventNode
                    {
                        Id       = nodeId,
                        Name     = segments[i],
                        Type     = wwiseType,
                        NodeType = nodeType
                    };

                    nodeCache[key] = node;

                    if (parent != null)
                        parent.Children.Add(node);
                    else
                        roots.Add(node);

                    // Map container nodes to WwiseObject for right-panel selection.
                    // TODO: Extend container object data when container-specific
                    //       browsing logic is implemented (e.g. fetch child sounds,
                    //       randomisation weights, switch assignments).
                    if (nodeType == WwiseNodeType.Container)
                    {
                        nodeToObjectMap[node] = new WwiseObject
                        {
                            Id    = nodeId,
                            Name  = segments[i],
                            Notes = intermediateInfo?["notes"]?.ToString() ?? string.Empty,
                            Type  = wwiseType,
                            Path  = "\\" + key
                        };
                    }
                }

                parent = node;
            }

            // Create the leaf Sound node.
            var eventNode = new WwiseEventNode
            {
                Id       = obj["id"]?.ToString()   ?? string.Empty,
                Name     = obj["name"]?.ToString() ?? string.Empty,
                Type     = obj["type"]?.ToString() ?? "Sound",
                NodeType = WwiseNodeType.Object
            };

            if (parent != null)
                parent.Children.Add(eventNode);
            else
                roots.Add(eventNode);

            // Map the Sound node to its corresponding WwiseObject for later use.
            nodeToObjectMap[eventNode] = new WwiseObject
            {
                Id       = obj["id"]?.ToString()   ?? string.Empty,
                Name     = obj["name"]?.ToString() ?? string.Empty,
                Notes    = obj["notes"]?.ToString() ?? string.Empty,
                Type     = obj["type"]?.ToString() ?? "Sound",
                Duration = obj["playbackDuration"]?["playbackDurationMax"]?.ToObject<double>() ?? 0.0,
                Path     = obj["path"]?.ToString() ?? string.Empty
            };
        }

        return roots;
    }

    // ── Private Helpers ─────────────────────────────────────────

    /// <summary>
    /// Classifies a Wwise object type string into the application's
    /// <see cref="WwiseNodeType"/> enum.
    /// </summary>
    private static WwiseNodeType ClassifyNodeType(string wwiseType)
    {
        if (FolderTypes.Contains(wwiseType))
            return WwiseNodeType.Folder;

        if (ContainerTypes.Contains(wwiseType))
            return WwiseNodeType.Container;

        // Default: treat unknown intermediate types as Folder
        // to maintain backward-compatible expandable behaviour.
        return WwiseNodeType.Folder;
    }

    /// <summary>
    /// Strips the leading backslash from a Wwise path for consistent
    /// dictionary key comparison.
    /// </summary>
    private static string NormalisePath(string path)
    {
        return path.TrimStart('\\');
    }
}