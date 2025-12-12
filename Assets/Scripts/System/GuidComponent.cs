using UnityEngine;

/// <summary>
/// Provides a persistent unique identifier for GameObjects.
/// The GUID is auto-generated once when the component is first added in the editor,
/// and NEVER changes after that - ensuring save/load systems can track this object forever.
/// </summary>
[System.Serializable]
[DisallowMultipleComponent]
public class GuidComponent : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Unique identifier - auto-generated once, never changes")]
    private string uniqueId;
    
    // Track if we've ever generated an ID (persists with prefab)
    [SerializeField, HideInInspector]
    private bool hasGeneratedId = false;

    /// <summary>
    /// Get the unique GUID for this object.
    /// </summary>
    public string GetGuid()
    {
        // Safety check for runtime - should never happen if editor setup correctly
        if (string.IsNullOrEmpty(uniqueId))
        {
            Debug.LogWarning($"[GuidComponent] {gameObject.name} has no GUID! Generating one at runtime (not ideal).", this);
            GenerateGuidInternal();
        }
        return uniqueId;
    }
    
    /// <summary>
    /// Check if this component has a valid GUID.
    /// </summary>
    public bool HasValidGuid => !string.IsNullOrEmpty(uniqueId);

    // Called when component is first added in editor, or when Reset is used
    private void Reset()
    {
        // Only generate if we've never had an ID before
        if (!hasGeneratedId)
        {
            GenerateGuidInternal();
#if UNITY_EDITOR
            Debug.Log($"[GuidComponent] Auto-generated GUID for {gameObject.name}: {uniqueId}");
#endif
        }
    }
    
    // Called when values are changed in inspector (editor only)
    private void OnValidate()
    {
        // Auto-generate if empty and we're in editor (not playing)
        // This catches cases where the component exists but has no ID
#if UNITY_EDITOR
        if (!Application.isPlaying && !hasGeneratedId && string.IsNullOrEmpty(uniqueId))
        {
            // Delay to avoid issues during serialization
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && !hasGeneratedId && string.IsNullOrEmpty(uniqueId))
                {
                    GenerateGuidInternal();
                    UnityEditor.EditorUtility.SetDirty(this);
                    Debug.Log($"[GuidComponent] Auto-generated GUID for {gameObject.name}: {uniqueId}");
                }
            };
        }
#endif
    }
    
    private void GenerateGuidInternal()
    {
        if (hasGeneratedId && !string.IsNullOrEmpty(uniqueId))
        {
            // Already have a valid ID, don't regenerate!
            return;
        }
        
        uniqueId = System.Guid.NewGuid().ToString();
        hasGeneratedId = true;
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>
    /// Force regenerate GUID. USE WITH EXTREME CAUTION - will break save references!
    /// Only use this for duplicating objects that need unique IDs.
    /// </summary>
    [ContextMenu("⚠️ Force Regenerate GUID (Breaks Saves!)")]
    public void ForceRegenerateGuid()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            string oldId = uniqueId;
            uniqueId = System.Guid.NewGuid().ToString();
            hasGeneratedId = true;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.LogWarning($"[GuidComponent] GUID regenerated for {gameObject.name}!\n  Old: {oldId}\n  New: {uniqueId}\n  ⚠️ Any saves referencing the old ID will be broken!", this);
        }
#endif
    }
    
    /// <summary>
    /// Called when duplicating a prefab instance - ensures unique ID
    /// </summary>
    public void EnsureUniqueIdForDuplicate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // When duplicating, we want a NEW unique ID
            uniqueId = System.Guid.NewGuid().ToString();
            hasGeneratedId = true;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[GuidComponent] Generated unique GUID for duplicated object {gameObject.name}: {uniqueId}");
        }
#endif
    }
}