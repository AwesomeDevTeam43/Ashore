using UnityEngine;

// Attach to existing teleports/portals. Provides a stable portalId
// and resolves a spawn position using either a Lab_Door exitPoint or
// an explicit override, else falls back to this transform position.
public class PortalAnchor : MonoBehaviour
{
    [Tooltip("Unique id used to match incoming spawns from previous scene.")]
    public string portalId;

    [Tooltip("Optional explicit spawn point. If null, uses Lab_Door.exitPoint if present, else this transform.")]
    public Transform spawnPointOverride;

    public Vector3 GetSpawnPosition()
    {
        if (spawnPointOverride != null) return spawnPointOverride.position;
        var door = GetComponent<Lab_Door>();
        if (door != null && door.exitPoint != null) return door.exitPoint.position;
        return transform.position;
    }
}
