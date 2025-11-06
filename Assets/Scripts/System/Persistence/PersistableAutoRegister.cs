using System.Collections.Generic;
using UnityEngine;

// Optional helper: attach to any GameObject that has one or more IPersistable components
// to auto-register them with GameState on enable/disable.
public class PersistableAutoRegister : MonoBehaviour
{
  private readonly List<IPersistable> _persistables = new List<IPersistable>();

  private void OnEnable()
  {
    _persistables.Clear();
    GetComponents(_persistables); // collect all IPersistable on this GameObject
    foreach (var p in _persistables)
    {
      GameState.Instance?.Register(p);
    }
  }

  private void OnDisable()
  {
    foreach (var p in _persistables)
    {
      GameState.Instance?.Unregister(p);
    }
    _persistables.Clear();
  }
}
