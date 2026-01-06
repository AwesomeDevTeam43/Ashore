using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a collection of <see cref="MonoBehaviour"/> targets disabled until the gate is explicitly unlocked.
/// </summary>
public class TutorialGatekeeper : MonoBehaviour
{
    [Tooltip("Scripts or components that should remain disabled until the gate is unlocked. Leave empty to affect the GameObject itself.")]
    [SerializeField] private MonoBehaviour[] componentsToToggle;
    [Tooltip("If true, toggle the GameObject active state instead of individual components.")]
    [SerializeField] private bool toggleGameObject = false;
    [Tooltip("Ensure the gate starts locked when the scene loads.")]
    [SerializeField] private bool startLocked = true;

    private readonly Dictionary<MonoBehaviour, bool> originalStates = new();
    private bool isLocked;

    private void Awake()
    {
        foreach (var comp in componentsToToggle)
        {
            if (comp == null) continue;
            originalStates[comp] = comp.enabled;
        }

        if (toggleGameObject)
        {
            originalStates[null] = gameObject.activeSelf;
        }

        if (startLocked)
        {
            Lock();
        }
    }

    /// <summary>
    /// Keeps everything disabled until <see cref="Unlock"/> is called.
    /// </summary>
    public void Lock()
    {
        if (isLocked) return;
        ApplyState(false);
        isLocked = true;
    }

    /// <summary>
    /// Restores the original enabled/active states.
    /// </summary>
    public void Unlock()
    {
        if (!isLocked) return;
        ApplyOriginalStates();
        isLocked = false;
    }

    private void ApplyState(bool enabled)
    {
        if (toggleGameObject)
        {
            gameObject.SetActive(enabled);
        }
        foreach (var comp in componentsToToggle)
        {
            if (comp == null) continue;
            comp.enabled = enabled;
        }
    }

    private void ApplyOriginalStates()
    {
        if (toggleGameObject && originalStates.TryGetValue(null, out var goState))
        {
            gameObject.SetActive(goState);
        }

        foreach (var comp in componentsToToggle)
        {
            if (comp == null) continue;
            if (originalStates.TryGetValue(comp, out var state))
            {
                comp.enabled = state;
            }
        }
    }

    /// <summary>
    /// Helper that returns whether the gate is currently preventing its targets from running.
    /// </summary>
    public bool IsLocked => isLocked;
}