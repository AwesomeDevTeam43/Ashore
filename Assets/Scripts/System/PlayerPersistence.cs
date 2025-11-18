using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

// Attach this to the Player root object to persist it across scenes
// and spawn it at a named SceneSpawnPoint when loading a new scene.
public class PlayerPersistence : MonoBehaviour
{
    private static PlayerPersistence instance;
    public static string nextSpawnId;
    private static bool destroyOnNextSceneLoad;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static void SetNextSpawn(string spawnId)
    {
        nextSpawnId = spawnId;
    }

    public static void DestroyPersistentPlayer()
    {
        if (instance == null) return;

        var go = instance.gameObject;
        instance = null;
        if (go != null)
        {
            Object.Destroy(go);
        }
    }

    public static void ScheduleDestroyOnNextSceneLoad()
    {
        destroyOnNextSceneLoad = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (destroyOnNextSceneLoad)
        {
            destroyOnNextSceneLoad = false;
            DestroyPersistentPlayer();
            return;
        }
        // Remove duplicates spawned by the new scene (keep this instance)
        var players = GameObject.FindGameObjectsWithTag("Player");
        Vector3? replacementPosition = null;
        Quaternion replacementRotation = Quaternion.identity;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (p == this.gameObject) continue;
            if (!replacementPosition.HasValue)
            {
                replacementPosition = p.transform.position;
                replacementRotation = p.transform.rotation;
            }
            Destroy(p);
        }

        // Warp to spawn point if specified
        bool warped = false;
        if (!string.IsNullOrEmpty(nextSpawnId))
        {
            // Priority 1: PortalAnchor (use existing portal's configured exit/position)
            warped = TryWarpToPortalAnchor(nextSpawnId);
            // Priority 2: SceneSpawnPoint (generic spawn marker)
            if (!warped) warped = TryWarpToSceneSpawnPoint(nextSpawnId);
            nextSpawnId = null; // consume
        }

        if (!warped && replacementPosition.HasValue)
        {
            WarpTo(replacementPosition.Value);
            transform.rotation = replacementRotation;
        }
    }

    private bool TryWarpToPortalAnchor(string spawnId)
    {
        var anchors = Object.FindObjectsByType<PortalAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var a in anchors)
        {
            if (a != null && a.portalId == spawnId)
            {
                Vector3 dest = a.GetSpawnPosition();
                WarpTo(dest);
                return true;
            }
        }
        return false;
    }

    private bool TryWarpToSceneSpawnPoint(string spawnId)
    {
        var points = Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sp in points)
        {
            if (sp != null && sp.spawnId == spawnId)
            {
                WarpTo(sp.transform.position);
                return true;
            }
        }
        return false;
    }

    private void WarpTo(Vector3 position)
    {
        Vector3 oldPos = transform.position;
        transform.position = position;
        var rb = GetComponent<Rigidbody2D>(); if (rb != null) rb.linearVelocity = Vector2.zero;

        // Nudge camera rigs so they don't lerp from old position
        Vector3 delta = transform.position - oldPos;
        var mainCam = Camera.main;
        CinemachineBrain brain = null;
        if (mainCam != null) brain = mainCam.GetComponent<CinemachineBrain>();
        if (brain != null) brain.enabled = false;
        var vcs = Object.FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsSortMode.None);
        foreach (var v in vcs)
        {
            if (v == null) continue;
            if (v.Follow == transform || v.LookAt == transform)
            {
                v.OnTargetObjectWarped(transform, delta);
            }
        }
        if (brain != null) brain.enabled = true;
    }
}
