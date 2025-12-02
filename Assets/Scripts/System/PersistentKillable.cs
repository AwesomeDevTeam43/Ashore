using System.Linq;
using UnityEngine;

[RequireComponent(typeof(GuidComponent))]
public class PersistentKillable : MonoBehaviour, ISaveable
{
    [SerializeField] private bool hideRenderersOnDeath = true;
    [SerializeField] private bool disableCollidersOnDeath = true;
    [SerializeField] private bool disableRigidbodiesOnDeath = true;
    [SerializeField] private MonoBehaviour[] componentsToDisableOnDeath;
    [SerializeField] private GameObject[] objectsToHideOnDeath;

    [SerializeField] private bool isDead = false;

    public bool IsDead => isDead;

    private void Awake()
    {
        var guid = GetComponent<GuidComponent>();
        if (guid != null && string.IsNullOrEmpty(guid.GetGuid()))
        {
            Debug.LogWarning($"PersistentKillable on '{name}' has empty GUID. Please generate a GUID in the editor.", this);
        }
    }

    public void MarkDead()
    {
        if (isDead) return;
        isDead = true;
        ApplyDeadState();
    }

    public void MarkAlive()
    {
        if (!isDead) return;
        isDead = false;
        ApplyAliveState();
    }

    private void ApplyDeadState()
    {
        var eh = GetComponent<Enemy_Health>();
        if (eh != null) eh.SetDamageable(false);

        if (disableCollidersOnDeath)
        {
            var cols = GetComponentsInChildren<Collider2D>(true);
            foreach (var c in cols) c.enabled = false;
        }

        if (disableRigidbodiesOnDeath)
        {
            var rbs = GetComponentsInChildren<Rigidbody2D>(true);
            foreach (var rb in rbs)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        if (hideRenderersOnDeath)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = false;
        }

        // Disable any explicitly listed components
        if (componentsToDisableOnDeath != null)
        {
            foreach (var mb in componentsToDisableOnDeath)
            {
                if (mb != null && mb != this) mb.enabled = false;
            }
        }
        // Additionally, disable all other behaviours on this object to stop AI/logic
        var allBehaviours = GetComponents<MonoBehaviour>();
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;
            if (mb == (MonoBehaviour)(object)this) continue;
            if (mb is GuidComponent) continue;
            // keep this script enabled so it can be saved
            mb.enabled = false;
        }

        if (objectsToHideOnDeath != null)
        {
            foreach (var go in objectsToHideOnDeath)
            {
                if (go != null) go.SetActive(false);
            }
        }
    }

    private void ApplyAliveState()
    {
        var eh = GetComponent<Enemy_Health>();
        if (eh != null) eh.SetDamageable(true);

        var cols = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) c.enabled = true;

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = true;

        var rbs = GetComponentsInChildren<Rigidbody2D>(true);
        foreach (var rb in rbs)
        {
            // leave body type unchanged; runtime may configure it
        }

        if (componentsToDisableOnDeath != null)
        {
            foreach (var mb in componentsToDisableOnDeath)
            {
                if (mb != null && mb != this) mb.enabled = true;
            }
        }
        // Re-enable other behaviours previously disabled
        var allBehaviours = GetComponents<MonoBehaviour>();
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;
            if (mb == (MonoBehaviour)(object)this) continue;
            if (mb is GuidComponent) continue;
            mb.enabled = true;
        }

        if (objectsToHideOnDeath != null)
        {
            foreach (var go in objectsToHideOnDeath)
            {
                if (go != null) go.SetActive(true);
            }
        }
    }

    public object CaptureState()
    {
        return isDead;
    }

    public void RestoreState(object state)
    {
        bool savedDead = false;
        if (state is bool b)
        {
            savedDead = b;
        }
        else if (state != null)
        {
            // attempt to parse from string if needed
            bool.TryParse(state.ToString(), out savedDead);
        }

        if (savedDead)
        {
            isDead = true;
            ApplyDeadState();
        }
        else
        {
            isDead = false;
            ApplyAliveState();
        }
    }
}
