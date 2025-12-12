using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Debug controller for genetic algorithm system.
/// Provides keybinds for toggling UI, respawning enemies, and other debug functions.
/// 
/// Keybinds:
/// - H: Toggle genome HUD visibility on all enemies
/// - R: Respawn all enemies (requires Left Ctrl held)
/// - G: Toggle main genetic debug panel
/// - Tab: Toggle detailed/compact view for enemy HUDs
/// - F5: Force evolution for all species
/// </summary>
public class GeneticDebugController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableDebugControls = true;
    [SerializeField] private bool showControlsHint = true;
    [SerializeField] private float hintDisplayTime = 3f;
    
    [Header("Keybinds")]
    [SerializeField] private KeyCode toggleHudKey = KeyCode.H;
    [SerializeField] private KeyCode respawnKey = KeyCode.R;
    [SerializeField] private KeyCode toggleDebugPanelKey = KeyCode.G;
    [SerializeField] private KeyCode toggleDetailedViewKey = KeyCode.Tab;
    [SerializeField] private KeyCode forceEvolveKey = KeyCode.F5;
    
    [Header("Respawn Settings")]
    [SerializeField] private bool requireModifierForRespawn = true;
    [SerializeField] private KeyCode respawnModifierKey = KeyCode.LeftControl;
    
    private float hintTimer = 0f;
    private string currentHint = "";
    private GUIStyle hintStyle;
    private bool hintStyleInitialized = false;
    
    // Cached references
    private Enemies_SpawnManager[] spawnManagers;
    private GlobalGeneticDebugUI debugUI;
    
    private void Awake()
    {
        // Cache references
        RefreshCaches();
    }
    
    private void OnEnable()
    {
        RefreshCaches();
    }
    
    private void RefreshCaches()
    {
        spawnManagers = FindObjectsByType<Enemies_SpawnManager>(FindObjectsSortMode.None);
        debugUI = FindAnyObjectByType<GlobalGeneticDebugUI>();
    }
    
    private void Update()
    {
        if (!enableDebugControls) return;
        
        // H - Toggle Genome HUD
        if (Input.GetKeyDown(toggleHudKey))
        {
            EnemyGenomeUI.ToggleAllUI();
            ShowHint($"Genome HUD: {(EnemyGenomeUI.IsGlobalUIVisible ? "ON" : "OFF")}");
        }
        
        // Ctrl+R - Respawn enemies
        if (Input.GetKeyDown(respawnKey))
        {
            bool modifierHeld = !requireModifierForRespawn || Input.GetKey(respawnModifierKey);
            if (modifierHeld)
            {
                RespawnAllEnemies();
                ShowHint("Enemies Respawned!");
            }
            else
            {
                ShowHint($"Hold {respawnModifierKey} + {respawnKey} to respawn");
            }
        }
        
        // G - Toggle debug panel
        if (Input.GetKeyDown(toggleDebugPanelKey))
        {
            if (debugUI != null)
            {
                debugUI.ToggleVisibility();
                ShowHint($"Debug Panel: {(debugUI.IsVisible ? "ON" : "OFF")}");
            }
            else
            {
                ShowHint("No GlobalGeneticDebugUI found");
            }
        }
        
        // Tab - Toggle detailed/compact view
        if (Input.GetKeyDown(toggleDetailedViewKey))
        {
            ToggleAllDetailedViews();
            ShowHint("Toggled HUD detail level");
        }
        
        // F5 - Force evolution
        if (Input.GetKeyDown(forceEvolveKey))
        {
            ForceEvolveAllSpecies();
            ShowHint("Forced evolution for all species!");
        }
        
        // Update hint timer
        if (hintTimer > 0)
        {
            hintTimer -= Time.unscaledDeltaTime;
        }
    }
    
    private void RespawnAllEnemies()
    {
        // First, destroy all existing enemies
        var existingEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        int destroyedCount = 0;
        
        foreach (var enemy in existingEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                Destroy(enemy.gameObject);
                destroyedCount++;
            }
        }
        
        Debug.Log($"[GeneticDebug] Destroyed {destroyedCount} enemies");
        
        // Refresh spawn managers and respawn
        RefreshCaches();
        
        // Reset spawn points and trigger respawn
        var spawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
        foreach (var sp in spawnPoints)
        {
            sp.hasSpawned = false;
        }
        
        // Trigger spawn managers
        int spawnedCount = 0;
        foreach (var manager in spawnManagers)
        {
            if (manager != null)
            {
                manager.RespawnAll();
                spawnedCount++;
            }
        }
        
        Debug.Log($"[GeneticDebug] Triggered {spawnedCount} spawn managers");
    }
    
    private void ToggleAllDetailedViews()
    {
        var genomeUIs = FindObjectsByType<EnemyGenomeUI>(FindObjectsSortMode.None);
        foreach (var ui in genomeUIs)
        {
            ui.ToggleDetailedView();
        }
    }
    
    private void ForceEvolveAllSpecies()
    {
        if (GlobalGeneticEvolver.Instance == null)
        {
            Debug.LogWarning("[GeneticDebug] GlobalGeneticEvolver not found");
            return;
        }
        
        var stats = GlobalGeneticEvolver.Instance.GetAllSpeciesStats();
        foreach (var species in stats.Keys)
        {
            GlobalGeneticEvolver.Instance.ForceEvolution(species);
        }
        
        Debug.Log($"[GeneticDebug] Forced evolution for {stats.Count} species");
    }
    
    private void ShowHint(string message)
    {
        currentHint = message;
        hintTimer = hintDisplayTime;
        Debug.Log($"[GeneticDebug] {message}");
    }
    
    private void InitHintStyle()
    {
        if (hintStyleInitialized) return;
        
        hintStyle = new GUIStyle(GUI.skin.box);
        hintStyle.fontSize = 14;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = Color.white;
        
        var bgTex = new Texture2D(2, 2);
        var bgColor = new Color(0, 0, 0, 0.8f);
        bgTex.SetPixels(new Color[] { bgColor, bgColor, bgColor, bgColor });
        bgTex.Apply();
        hintStyle.normal.background = bgTex;
        hintStyle.padding = new RectOffset(15, 15, 10, 10);
        
        hintStyleInitialized = true;
    }
    
    private void OnGUI()
    {
        if (!enableDebugControls) return;
        
        InitHintStyle();
        
        // Show controls hint at top-left
        if (showControlsHint)
        {
            string controlsText = $"[{toggleHudKey}] HUD  [{respawnModifierKey}+{respawnKey}] Respawn  [{toggleDebugPanelKey}] Debug  [{forceEvolveKey}] Evolve";
            GUI.Label(new Rect(10, 10, 500, 25), controlsText, hintStyle);
        }
        
        // Show action hint at center
        if (hintTimer > 0 && !string.IsNullOrEmpty(currentHint))
        {
            float alpha = Mathf.Clamp01(hintTimer);
            var oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            
            var content = new GUIContent(currentHint);
            var size = hintStyle.CalcSize(content);
            float x = (Screen.width - size.x) / 2;
            float y = Screen.height * 0.3f;
            
            GUI.Label(new Rect(x, y, size.x + 30, size.y + 10), currentHint, hintStyle);
            
            GUI.color = oldColor;
        }
    }
}
