using UnityEngine;

// Place this in scenes to mark where the persistent player should appear.
public class SceneSpawnPoint : MonoBehaviour
{
    [Tooltip("Unique ID for this spawn point within the scene.")]
    public string spawnId;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.85f);
        Gizmos.DrawSphere(transform.position, 0.15f);
        if (!string.IsNullOrEmpty(spawnId))
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.25f, $"Spawn: {spawnId}");
        }
    }
#endif
}
