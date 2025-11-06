using UnityEngine;

// Components implementing this can have their state saved/restored across transitions
public interface IPersistable
{
  // Unique key per object; include scene or GUID if needed. For now, use a stable identifier on the object.
  string GetPersistenceKey();

  // Return a plain serializable object representing current state (POCO)
  object CaptureState();

  // Apply a state object captured previously
  void RestoreState(object state);
}
