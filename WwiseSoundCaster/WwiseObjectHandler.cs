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
    // ── Public API ──────────────────────────────────────────────

    /// <summary>
    /// Queries the connected Wwise project for all Event-typed objects
    /// and builds a hierarchical tree grouped by their Wwise path segments.
    /// </summary>
    /// <returns>
    /// Top-level <see cref="WwiseEventNode"/> list (folders and events).
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
            var args = new JObject
            {
                {"waql", "from type Sound"},
            };

            var options = new JObject
            {
                {"return", new JArray("id", "name", "notes", "type", "playbackDuration", "path", "extractEvents")}
            };

            var resultJson = await WwiseClient.client.Call(
                ak.wwise.core.@object.get, args, options);

            // Console.WriteLine($"[WwiseObjectHandler] {resultJson["return"]?.ToString()}");

            var objects = resultJson["return"] as JArray;

            // Console.WriteLine($"[WwiseObjectHandler] Parsed {objects?.Count} objects from WAAPI result.");

            if (objects == null || objects.Count == 0)
            {
                return Array.Empty<WwiseEventNode>();
            }

            return BuildHierarchy(objects);
        }
        catch (Exception ex)
        {
            // TODO: Replace Console with ILogger when a logging framework is added.
            Console.WriteLine($"[WwiseObjectHandler] Fetch failed: {ex.Message}");
            return Array.Empty<WwiseEventNode>();
        }
    }

    // ── Hierarchy Builder ───────────────────────────────────────

    /// <summary>
    /// Transforms a flat list of WAAPI objects (each with a <c>path</c> field)
    /// into a nested <see cref="WwiseEventNode"/> tree.
    ///
    /// Example path: <c>\Events\Default Work Unit\Footsteps\Play_Footstep</c>
    ///   → Folder "Default Work Unit"
    ///       → Folder "Footsteps"
    ///           → Event "Play_Footstep"
    /// </summary>
    private static IReadOnlyList<WwiseEventNode> BuildHierarchy(JArray objects)
    {
        // Cache of already-created folder nodes keyed by their full
        // path prefix so we never duplicate intermediate folders.
        var folderCache = new Dictionary<string, WwiseEventNode>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<WwiseEventNode>();

        foreach (var obj in objects)
        {
            var fullPath = obj["path"]?.ToString() ?? string.Empty;
            var segments = fullPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            // A valid event path has at least the root category ("Events")
            // and the event name — skip malformed entries.
            if (segments.Length < 2) continue;

            // Walk the intermediate folder segments (skip index 0 which is
            // the root "Events" category — we do not show it as a node).
            WwiseEventNode? parent = null;

            for (int i = 1; i < segments.Length - 1; i++)
            {
                var key = string.Join('\\', segments, 0, i + 1);

                if (!folderCache.TryGetValue(key, out var folder))
                {
                    folder = new WwiseEventNode
                    {
                        Name     = segments[i],
                        Type     = "Folder",
                        IsFolder = true
                    };
                    folderCache[key] = folder;

                    if (parent != null)
                        parent.Children.Add(folder);
                    else
                        roots.Add(folder);
                }

                parent = folder;
            }

            // Create the leaf event node.
            var eventNode = new WwiseEventNode
            {
                Id       = obj["id"]?.ToString()   ?? string.Empty,
                Name     = obj["name"]?.ToString() ?? string.Empty,
                Type     = obj["type"]?.ToString() ?? "Event",
                IsFolder = false
            };

            if (parent != null)
                parent.Children.Add(eventNode);
            else
                roots.Add(eventNode);

            // Map the event node to its corresponding Wwise object for later use.
            nodeToObjectMap[eventNode] = new WwiseObject
            {
                Id       = obj["id"]?.ToString()   ?? string.Empty,
                Name     = obj["name"]?.ToString() ?? string.Empty,
                Notes    = obj["notes"]?.ToString() ?? string.Empty,
                Type     = obj["type"]?.ToString() ?? "Event",
                Duration = obj["playbackDuration"]?["playbackDurationMax"]?.ToObject<double>() ?? 0.0,
                Path     = obj["path"]?.ToString() ?? string.Empty
            };
        }

        return roots;
    }
}