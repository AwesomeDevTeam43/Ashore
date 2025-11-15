using UnityEngine;
using UnityEngine.UI;

// Attach this to any UI element (with a Selectable) you want to be reachable by
// keyboard/controller navigation when a UINavScope is active.
[RequireComponent(typeof(Selectable))]
public class UINavTarget : MonoBehaviour
{
    // Marker component – no logic required. Kept for clarity and future per-item settings.
}
