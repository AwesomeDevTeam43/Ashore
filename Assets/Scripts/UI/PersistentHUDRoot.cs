using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Attach to your HUD Canvas root. Keeps the HUD across scenes and
// ensures a single EventSystem exists. Also nudges HUD to rebind to
// the current Player after each scene load.
public class PersistentHUDRoot : MonoBehaviour
{
    private static PersistentHUDRoot instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureEventSystem();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystem();
        DeduplicateHUDs();
        RebindHUD();
    }

    private void EnsureEventSystem()
    {
        // Prefer an EventSystem under this HUD; otherwise adopt the first one; otherwise create one under HUD.
        var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem ours = GetComponentInChildren<EventSystem>(includeInactive: true);
        if (ours == null)
        {
            if (all.Length == 0)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem)
#if ENABLE_INPUT_SYSTEM
                    , typeof(InputSystemUIInputModule)
#else
                    , typeof(StandaloneInputModule)
#endif
                );
                go.transform.SetParent(transform, false);
                ours = go.GetComponent<EventSystem>();
            }
            else
            {
                // Adopt the first existing one by moving it under HUD
                ours = all[0];
                if (ours != null)
                {
                    ours.transform.SetParent(transform, false);
                }
            }
        }
        // Ensure it has a compatible input module
#if ENABLE_INPUT_SYSTEM
        if (ours != null && ours.GetComponent<InputSystemUIInputModule>() == null)
        {
            var legacy = ours.GetComponent<StandaloneInputModule>();
            if (legacy != null) Destroy(legacy);
            if (ours.GetComponent<InputSystemUIInputModule>() == null)
                ours.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (ours != null && ours.GetComponent<StandaloneInputModule>() == null)
        {
            if (ours.GetComponent<StandaloneInputModule>() == null)
                ours.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
        // Remove duplicates (keep 'ours')
        foreach (var es in all)
        {
            if (es == null) continue;
            if (es == ours) continue;
            Object.Destroy(es.gameObject);
        }
    }

    private void RebindHUD()
    {
        // Ask any Manage_UI components under this HUD to refresh bindings
        var managers = GetComponentsInChildren<Manage_UI>(includeInactive: true);
        foreach (var m in managers)
        {
            if (m == null) continue;
            try { m.RebindAndRefresh(); } catch {}
        }
    }

    private void DeduplicateHUDs()
    {
        // Destroy any Manage_UI instances not under this persistent HUD root
        var allManagers = Object.FindObjectsByType<Manage_UI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in allManagers)
        {
            if (m == null) continue;
            if (m.transform.IsChildOf(this.transform)) continue; // keep ours

            // Prefer destroying the top-level Canvas that owns this HUD
            var canvas = m.GetComponentInParent<Canvas>();
            if (canvas != null && !canvas.transform.IsChildOf(this.transform))
            {
                Object.Destroy(canvas.gameObject);
            }
            else
            {
                Object.Destroy(m.gameObject);
            }
        }

        // Destroy any MenuController (menu/inventory UI) not under this persistent HUD root
        var allMenus = Object.FindObjectsByType<MenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mc in allMenus)
        {
            if (mc == null) continue;
            if (mc.transform.IsChildOf(this.transform)) continue; // keep ours

            // If it has an assigned menuRoot, destroy that container; else destroy its parent Canvas or self
            var menuRootField = mc.GetType().GetField("menuRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject menuRootGO = menuRootField != null ? (GameObject)menuRootField.GetValue(mc) : null;
            if (menuRootGO != null && !menuRootGO.transform.IsChildOf(this.transform))
            {
                Object.Destroy(menuRootGO);
                continue;
            }
            var c = mc.GetComponentInParent<Canvas>();
            if (c != null && !c.transform.IsChildOf(this.transform))
            {
                Object.Destroy(c.gameObject);
            }
            else
            {
                Object.Destroy(mc.gameObject);
            }
        }

        // Destroy stray InventoryPage or EquipmentPage instances not under this root (covers scenes with MenuCanvas prefabs)
        var invPages = Object.FindObjectsByType<InventoryPage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ip in invPages)
        {
            if (ip == null) continue;
            if (ip.transform.IsChildOf(this.transform)) continue;
            var c = ip.GetComponentInParent<Canvas>();
            if (c != null && !c.transform.IsChildOf(this.transform)) Object.Destroy(c.gameObject); else Object.Destroy(ip.gameObject);
        }

        var equipPages = Object.FindObjectsByType<EquipmentPage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ep in equipPages)
        {
            if (ep == null) continue;
            if (ep.transform.IsChildOf(this.transform)) continue;
            var c = ep.GetComponentInParent<Canvas>();
            if (c != null && !c.transform.IsChildOf(this.transform)) Object.Destroy(c.gameObject); else Object.Destroy(ep.gameObject);
        }
    }
}
