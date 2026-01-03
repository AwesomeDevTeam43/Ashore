using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central database holding all lore entries in the game.
/// Create one via Assets > Create > Lore > Lore Database.
/// </summary>
[CreateAssetMenu(fileName = "LoreDatabase", menuName = "Lore/Lore Database", order = 2)]
public class LoreDatabase : ScriptableObject
{
    [Header("All Lore Entries")]
    [Tooltip("Add all LoreEntry assets here. They will be indexed by their entryId.")]
    public List<LoreEntry> entries = new List<LoreEntry>();

    private Dictionary<string, LoreEntry> _lookup;

    /// <summary>
    /// Builds or returns the cached lookup dictionary.
    /// </summary>
    private Dictionary<string, LoreEntry> Lookup
    {
        get
        {
            if (_lookup == null || _lookup.Count != entries.Count)
            {
                RebuildLookup();
            }
            return _lookup;
        }
    }

    /// <summary>
    /// Rebuilds the internal dictionary from the entries list.
    /// Call this if you modify entries at runtime.
    /// </summary>
    public void RebuildLookup()
    {
        _lookup = new Dictionary<string, LoreEntry>();
        foreach (var entry in entries)
        {
            if (entry == null) continue;
            if (string.IsNullOrEmpty(entry.entryId))
            {
                Debug.LogWarning($"[LoreDatabase] Entry '{entry.name}' has no entryId set!");
                continue;
            }
            if (_lookup.ContainsKey(entry.entryId))
            {
                Debug.LogWarning($"[LoreDatabase] Duplicate entryId '{entry.entryId}' found! Skipping duplicate.");
                continue;
            }
            _lookup[entry.entryId] = entry;
        }
    }

    /// <summary>
    /// Gets a lore entry by its unique ID.
    /// </summary>
    public LoreEntry GetEntry(string entryId)
    {
        if (string.IsNullOrEmpty(entryId)) return null;
        Lookup.TryGetValue(entryId, out var entry);
        return entry;
    }

    /// <summary>
    /// Checks if an entry with the given ID exists.
    /// </summary>
    public bool HasEntry(string entryId)
    {
        if (string.IsNullOrEmpty(entryId)) return false;
        return Lookup.ContainsKey(entryId);
    }

    /// <summary>
    /// Gets all entry IDs in the database.
    /// </summary>
    public IEnumerable<string> GetAllEntryIds()
    {
        return Lookup.Keys;
    }

    private void OnValidate()
    {
        // Rebuild lookup when modified in editor
        RebuildLookup();
    }
}
