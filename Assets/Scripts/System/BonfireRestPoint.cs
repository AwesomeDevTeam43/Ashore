using UnityEngine;
using UnityEngine.InputSystem;

// A reusable rest point that heals the player and acts as a return/save point.
// On first activation it spawns a particle system and stays lit across saves/loads.
// Requires a GuidComponent so its lit state can be persisted by SaveSystem.
[RequireComponent(typeof(Collider2D))]
public class BonfireRestPoint : MonoBehaviour, ISaveable
{
    [Header("Visual")]
    [Tooltip("Particle system prefab to spawn when the bonfire is lit.")]
    [SerializeField] private ParticleSystem fireParticlePrefab;
    [Tooltip("Transform position where the particle system will spawn. If null, uses this object's position.")]
    [SerializeField] private Transform particleSpawnPoint;

    private ParticleSystem spawnedParticleInstance;

    [Header("Behavior")]
    [Tooltip("Optional return point override. If null, the bonfire's own transform is used.")]
    [SerializeField] private Transform returnPoint;
    [Tooltip("If true, calling Interact while in range will save and heal the player.")]
    [SerializeField] private bool enableInteractActivation = true;

    [Header("Persistence")]
    [Tooltip("Unique identifier holder used by SaveSystem. Make sure one exists on this object.")]
    [SerializeField] private GuidComponent guidComponent;

    private bool isLit = false;
    private bool playerInRange = false;

    private void Reset()
    {
        // Try to ensure trigger
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        // Try get GuidComponent if present
        if (guidComponent == null) guidComponent = GetComponent<GuidComponent>();
    }

    private void Awake()
    {
        if (guidComponent == null) guidComponent = GetComponent<GuidComponent>();
        ApplyVisual();
    }

    private void OnEnable()
    {
        ApplyVisual();
    }

    private void Update()
    {
        if (!enableInteractActivation || !playerInRange) return;

        if (IsInteractPressedThisFrame())
        {
            Activate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private bool IsInteractPressedThisFrame()
    {
        // Prefer the player's input handler if available (uses the "Interact" action)
        var player = FindPlayer();
        if (player != null)
        {
            var ih = player.GetComponent<Player_InputHandler>();
            if (ih != null && ih.InteractActionTriggered) return true;
        }

        // Fallbacks: direct input checks so it still works without wiring
        var kb = Keyboard.current;
        if (kb != null)
        {
            // Only accept explicit interact keys, avoid UI submit keys (Enter/Space)
            if (kb.fKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
                return true;
        }
        var gp = Gamepad.current;
        if (gp != null)
        {
            // Proper interact fallback: ButtonEast (e.g., B/Circle)
            if (gp.buttonEast.wasPressedThisFrame)
                return true;
        }
        // No UI-submit fallbacks here to avoid action map confusion
        return false;
    }

    private void Activate()
    {
        // Mark lit (persists) and update visuals
        if (!isLit)
        {
            isLit = true;
            ApplyVisual();
        }

        var player = FindPlayer();
        if (player != null)
        {
            // Heal player to full
            var ph = player.GetComponent<Player_Health>();
            var hs = player.GetComponent<HealthSystem>();
            if (ph != null && hs != null)
            {
                ph.SetHealth(hs.MaxHealth);
            }

            // Set return point
            Vector3 rp = returnPoint != null ? returnPoint.position : transform.position;
            ReturnPointManager.SetReturnPoint(rp);

            // Save game (persists bonfire state and player data)
            var pc = player.GetComponent<Player_Controller>();
            var xp = player.GetComponent<XP_System>();

            Debug.Log($"Bonfire '{name}' activated. Lit={isLit}. Return point set to {rp}. Saving via {(pc != null ? "Player_Controller.SaveGame()" : "SaveSystem.SavePlayer()")}.", this);
            if (pc != null)
            {
                pc.SaveGame();
            }
            else
            {
                // Defensive: fall back to SaveSystem if Player_Controller not found
                SaveSystem.SavePlayer(pc, xp, ph, Inventory.instance, SaveSlotTracker.CurrentSlot);
            }
        }
    }

    private GameObject FindPlayer()
    {
        return GameObject.FindGameObjectWithTag("Player");
    }

    private void ApplyVisual()
    {
        if (isLit && fireParticlePrefab != null && spawnedParticleInstance == null)
        {
            // Spawn and play the particle system at the designated transform or fallback to this object's position
            Vector3 spawnPosition = particleSpawnPoint != null ? particleSpawnPoint.position : transform.position;
            spawnedParticleInstance = Instantiate(fireParticlePrefab, spawnPosition, fireParticlePrefab.transform.rotation, transform);
            spawnedParticleInstance.Play();
        }
        else if (!isLit && spawnedParticleInstance != null)
        {
            // Destroy the particle system if bonfire is no longer lit
            Destroy(spawnedParticleInstance.gameObject);
            spawnedParticleInstance = null;
        }
    }

    // ISaveable implementation
    public object CaptureState()
    {
        return new BonfireData { isLit = this.isLit };
    }

    public void RestoreState(object state)
    {
        var data = (BonfireData)state;
        isLit = data.isLit;
        ApplyVisual();
    }

    [System.Serializable]
    private struct BonfireData
    {
        public bool isLit;
    }
}
