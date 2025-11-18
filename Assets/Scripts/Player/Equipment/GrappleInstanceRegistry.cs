using UnityEngine;

/// <summary>
/// Simple global tracker used to cap how many grappling hook projectiles/anchors can exist at once.
/// </summary>
public static class GrappleInstanceRegistry
{
    private static int activeProjectiles;
    private static int activeAnchors;

    public static int ActiveProjectiles => activeProjectiles;
    public static int ActiveAnchors => activeAnchors;
    public static int ActiveTotal => activeProjectiles + activeAnchors;

    public static void RegisterProjectile()
    {
        activeProjectiles++;
    }

    public static void UnregisterProjectile()
    {
        activeProjectiles = Mathf.Max(0, activeProjectiles - 1);
    }

    public static void RegisterAnchor()
    {
        activeAnchors++;
    }

    public static void UnregisterAnchor()
    {
        activeAnchors = Mathf.Max(0, activeAnchors - 1);
    }
}
