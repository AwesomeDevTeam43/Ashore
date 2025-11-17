using System;
using System.Collections.Generic;
using UnityEngine;

// Simple global store for persistable state across transitions and scenes.
// Future-proof: could be replaced by a proper save system.
public class GameState : MonoBehaviour
{
  public static GameState Instance { get; private set; }

  [Serializable]
  private class PersistedEntry
  {
    public string typeName; // AssemblyQualifiedName of the state object
    public string json;     // JsonUtility-serialized payload
  }

  private readonly Dictionary<string, PersistedEntry> _state = new Dictionary<string, PersistedEntry>();
  private readonly HashSet<IPersistable> _registered = new HashSet<IPersistable>();

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  public void Register(IPersistable p)
  {
    if (p != null) _registered.Add(p);
  }

  public void Unregister(IPersistable p)
  {
    if (p != null) _registered.Remove(p);
  }

  // Capture all registered object states
  public void CaptureAll()
  {
    foreach (var p in _registered)
    {
      try
      {
        var key = p.GetPersistenceKey();
        var state = p.CaptureState();
        if (state == null || string.IsNullOrEmpty(key)) continue;
        var entry = new PersistedEntry
        {
          typeName = state.GetType().AssemblyQualifiedName,
          json = JsonUtility.ToJson(state)
        };
        _state[key] = entry;
      }
      catch (Exception e)
      {
        Debug.LogWarning($"GameState: Failed to capture state for {p}: {e.Message}");
      }
    }
  }

  // Restore any saved states to matching registered objects
  public void RestoreAll()
  {
    foreach (var p in _registered)
    {
      try
      {
        var key = p.GetPersistenceKey();
        if (string.IsNullOrEmpty(key) || !_state.TryGetValue(key, out var entry)) continue;
        var t = Type.GetType(entry.typeName);
        if (t == null)
        {
          Debug.LogWarning($"GameState: Unknown state type {entry.typeName} for key {key}");
          continue;
        }
        var obj = JsonUtility.FromJson(entry.json, t);
        p.RestoreState(obj);
      }
      catch (Exception e)
      {
        Debug.LogWarning($"GameState: Failed to restore state for {p}: {e.Message}");
      }
    }
  }

  public void ClearAll()
  {
    _state.Clear();
  }
}
