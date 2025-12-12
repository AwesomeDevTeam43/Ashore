using UnityEngine;

/// <summary>
/// Floating UI that shows genetic stats above each enemy.
/// Automatically added to enemies with EnemyFitnessTracker.
/// Toggle visibility with keybind (see GeneticDebugController).
/// </summary>
public class EnemyGenomeUI : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private bool showDetailedView = true;
    
    [Header("Visual Settings")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color headerColor = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color speciesColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private int fontSize = 10;
    
    // Static toggle for all instances
    private static bool globalShowUI = true;
    
    private EnemyFitnessTracker fitnessTracker;
    private Camera mainCamera;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle speciesStyle;
    private GUIStyle compactStyle;
    private Texture2D backgroundTexture;
    private Texture2D barBackgroundTexture;
    private bool stylesInitialized = false;
    private bool _hasLoggedOnGUI = false;
    private bool _hasLoggedDrawing = false;
    private bool _hasLoggedCameraError = false;
    
    // Cached textures for gene bars to avoid per-frame allocation
    private Texture2D[] cachedBarTextures;
    
    private void Awake()
    {
        fitnessTracker = GetComponent<EnemyFitnessTracker>();
        mainCamera = Camera.main;
        Debug.Log($"[EnemyGenomeUI] Awake on {gameObject.name} - tracker: {(fitnessTracker != null ? "found" : "NULL")}, camera: {(mainCamera != null ? "found" : "NULL")}");
    }
    
    private void Start()
    {
        if (fitnessTracker == null)
        {
            fitnessTracker = GetComponent<EnemyFitnessTracker>();
        }
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        // Create textures
        backgroundTexture = MakeTexture(2, 2, backgroundColor);
        barBackgroundTexture = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.9f));
        
        // Pre-create bar textures for each gene type
        cachedBarTextures = new Texture2D[]
        {
            MakeTexture(2, 2, Color.red),      // HP
            MakeTexture(2, 2, new Color(0.3f, 0.5f, 1f)),  // DMG
            MakeTexture(2, 2, Color.cyan),     // SPD
            MakeTexture(2, 2, Color.yellow),   // ATK
            MakeTexture(2, 2, Color.magenta),  // AGR
            MakeTexture(2, 2, Color.green),    // RES
        };
        
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = backgroundTexture;
        boxStyle.padding = new RectOffset(6, 6, 6, 6);
        
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;
        labelStyle.normal.textColor = textColor;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        
        headerStyle = new GUIStyle(labelStyle);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = headerColor;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        speciesStyle = new GUIStyle(labelStyle);
        speciesStyle.fontStyle = FontStyle.Bold;
        speciesStyle.fontSize = fontSize + 1;
        speciesStyle.normal.textColor = speciesColor;
        speciesStyle.alignment = TextAnchor.MiddleCenter;
        
        compactStyle = new GUIStyle(labelStyle);
        compactStyle.fontSize = fontSize - 1;
        compactStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        
        stylesInitialized = true;
    }
    
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
    
    private void OnGUI()
    {
        // One-time debug log
        if (!_hasLoggedOnGUI)
        {
            Debug.Log($"[EnemyGenomeUI] OnGUI called on {gameObject.name} - showUI:{showUI}, globalShowUI:{globalShowUI}");
            _hasLoggedOnGUI = true;
        }
        
        // Early exit conditions with debug info
        if (!showUI || !globalShowUI) return;
        
        // Try multiple ways to get the camera
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) 
        {
            // Try finding any camera tagged MainCamera
            var cameras = Camera.allCameras;
            foreach (var cam in cameras)
            {
                if (cam.CompareTag("MainCamera"))
                {
                    mainCamera = cam;
                    break;
                }
            }
        }
        if (mainCamera == null && Camera.allCamerasCount > 0)
        {
            // Just use the first active camera
            mainCamera = Camera.allCameras[0];
        }
        
        if (mainCamera == null) 
        {
            if (!_hasLoggedCameraError)
            {
                Debug.LogWarning($"[EnemyGenomeUI] {gameObject.name}: No camera found! Total cameras: {Camera.allCamerasCount}");
                _hasLoggedCameraError = true;
            }
            return;
        }
        
        // Check distance from camera
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        if (distance > maxDistance) 
        {
            // Don't log every frame, too spammy
            return;
        }
        
        // Convert world to screen position
        Vector3 worldPos = transform.position + offset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        // Check if behind camera
        if (screenPos.z < 0) 
        {
            // Behind camera, don't draw
            return;
        }
        
        // We're about to draw! Log once per enemy
        if (!_hasLoggedDrawing)
        {
            Debug.Log($"[EnemyGenomeUI] DRAWING UI for {gameObject.name} at screen pos ({screenPos.x:F0}, {screenPos.y:F0}), camera: {mainCamera.name}");
            _hasLoggedDrawing = true;
        }
        
        // Simple test draw - a red box that should ALWAYS appear if OnGUI is working
        // Comment this out once UI is confirmed working
        GUI.color = Color.red;
        GUI.Box(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 30, 100, 25), gameObject.name);
        GUI.color = Color.white;
        
        InitStyles();
        
        // Convert to GUI coordinates (Y inverted)
        float guiY = Screen.height - screenPos.y;
        
        // Handle missing tracker or genome - show error state
        if (fitnessTracker == null)
        {
            DrawErrorPanel(screenPos.x, guiY, "No Tracker");
            return;
        }
        
        if (fitnessTracker.Genome == null)
        {
            DrawErrorPanel(screenPos.x, guiY, $"No Genome\n({fitnessTracker.Species})");
            return;
        }
        
        var genome = fitnessTracker.Genome;
        var species = fitnessTracker.Species;
        int generation = GlobalGeneticEvolver.Instance?.GetGeneration(species) ?? 0;
        
        if (showDetailedView)
        {
            DrawDetailedPanel(screenPos.x, guiY, genome, species, generation);
        }
        else
        {
            DrawCompactPanel(screenPos.x, guiY, genome, species, generation);
        }
    }
    
    private void DrawErrorPanel(float screenX, float guiY, string message)
    {
        float panelWidth = 100;
        float panelHeight = 40;
        float x = screenX - panelWidth / 2;
        float y = guiY - panelHeight;
        
        x = Mathf.Clamp(x, 5, Screen.width - panelWidth - 5);
        y = Mathf.Clamp(y, 5, Screen.height - panelHeight - 5);
        
        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "", boxStyle);
        
        var errorStyle = new GUIStyle(labelStyle);
        errorStyle.normal.textColor = Color.red;
        errorStyle.alignment = TextAnchor.MiddleCenter;
        errorStyle.fontSize = 9;
        
        GUI.Label(new Rect(x, y + 5, panelWidth, panelHeight - 10), message, errorStyle);
    }
    
    private void DrawDetailedPanel(float screenX, float guiY, EnemyGenome genome, EnemySpecies species, int generation)
    {
        // Panel dimensions
        float panelWidth = 160;
        float panelHeight = 175;
        float x = screenX - panelWidth / 2;
        float y = guiY - panelHeight;
        
        // Keep on screen
        x = Mathf.Clamp(x, 5, Screen.width - panelWidth - 5);
        y = Mathf.Clamp(y, 5, Screen.height - panelHeight - 5);
        
        // Draw panel background
        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "", boxStyle);
        
        float lineY = y + 5;
        float lineHeight = 14;
        float barHeight = 8;
        float labelWidth = 45;
        float barWidth = panelWidth - labelWidth - 30;
        
        // Species name header
        string speciesName = EnemySpeciesHelper.GetDisplayName(species);
        GUI.Label(new Rect(x, lineY, panelWidth, lineHeight + 2), speciesName, speciesStyle);
        lineY += lineHeight + 2;
        
        // Generation and Power Level
        float powerLevel = genome.GetPowerLevel();
        string powerColor = powerLevel < 0.4f ? "<color=#88ff88>" : powerLevel < 0.7f ? "<color=#ffff88>" : "<color=#ff8888>";
        GUI.Label(new Rect(x + 5, lineY, panelWidth - 10, lineHeight), 
            $"Gen {generation}  |  Power: {powerLevel:P0}", compactStyle);
        lineY += lineHeight + 4;
        
        // Gene bars with cached textures
        DrawGeneBarCached(x + 5, ref lineY, "HP", genome.healthGene, labelWidth, barWidth, barHeight, 0);
        DrawGeneBarCached(x + 5, ref lineY, "DMG", genome.damageGene, labelWidth, barWidth, barHeight, 1);
        DrawGeneBarCached(x + 5, ref lineY, "SPD", genome.movementSpeedGene, labelWidth, barWidth, barHeight, 2);
        DrawGeneBarCached(x + 5, ref lineY, "ATK", genome.attackSpeedGene, labelWidth, barWidth, barHeight, 3);
        DrawGeneBarCached(x + 5, ref lineY, "AGR", genome.aggressivenessGene, labelWidth, barWidth, barHeight, 4);
        DrawGeneBarCached(x + 5, ref lineY, "RES", (genome.meleeResistanceGene + genome.rangedResistanceGene) / 2f, labelWidth, barWidth, barHeight, 5);
        
        lineY += 4;
        
        // Current combat stats
        GUI.Label(new Rect(x + 5, lineY, panelWidth - 10, lineHeight), 
            $"DMG Dealt: {fitnessTracker.DamageDealtToPlayer:F0}", labelStyle);
        lineY += lineHeight;
        
        GUI.Label(new Rect(x + 5, lineY, panelWidth - 10, lineHeight), 
            $"Alive: {fitnessTracker.SurvivalTime:F1}s", labelStyle);
    }
    
    private void DrawCompactPanel(float screenX, float guiY, EnemyGenome genome, EnemySpecies species, int generation)
    {
        float panelWidth = 100;
        float panelHeight = 45;
        float x = screenX - panelWidth / 2;
        float y = guiY - panelHeight;
        
        x = Mathf.Clamp(x, 5, Screen.width - panelWidth - 5);
        y = Mathf.Clamp(y, 5, Screen.height - panelHeight - 5);
        
        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "", boxStyle);
        
        string speciesName = EnemySpeciesHelper.GetDisplayName(species);
        float powerLevel = genome.GetPowerLevel();
        
        GUI.Label(new Rect(x, y + 5, panelWidth, 14), speciesName, speciesStyle);
        GUI.Label(new Rect(x + 5, y + 22, panelWidth - 10, 14), 
            $"G{generation} | {powerLevel:P0}", compactStyle);
    }
    
    private void DrawGeneBarCached(float x, ref float y, string label, float value, float labelWidth, float barWidth, float barHeight, int textureIndex)
    {
        // Label
        GUI.Label(new Rect(x, y, labelWidth, 14), label, labelStyle);
        
        // Background bar
        GUI.DrawTexture(new Rect(x + labelWidth, y + 3, barWidth, barHeight), barBackgroundTexture);
        
        // Filled bar using cached texture
        if (cachedBarTextures != null && textureIndex < cachedBarTextures.Length)
        {
            GUI.DrawTexture(new Rect(x + labelWidth, y + 3, barWidth * Mathf.Clamp01(value), barHeight), cachedBarTextures[textureIndex]);
        }
        
        // Value text
        GUI.Label(new Rect(x + labelWidth + barWidth + 2, y, 30, 14), $"{value:F2}", labelStyle);
        
        y += 14;
    }
    
    private void OnDestroy()
    {
        // Clean up textures
        if (backgroundTexture != null) Destroy(backgroundTexture);
        if (barBackgroundTexture != null) Destroy(barBackgroundTexture);
        
        if (cachedBarTextures != null)
        {
            foreach (var tex in cachedBarTextures)
            {
                if (tex != null) Destroy(tex);
            }
        }
    }
    
    /// <summary>
    /// Toggle UI visibility for this instance
    /// </summary>
    public void ToggleUI()
    {
        showUI = !showUI;
    }
    
    /// <summary>
    /// Set UI visibility for this instance
    /// </summary>
    public void SetVisible(bool visible)
    {
        showUI = visible;
    }
    
    /// <summary>
    /// Toggle global UI visibility for all instances
    /// </summary>
    public static void ToggleAllUI()
    {
        globalShowUI = !globalShowUI;
    }
    
    /// <summary>
    /// Set global UI visibility for all instances
    /// </summary>
    public static void SetAllVisible(bool visible)
    {
        globalShowUI = visible;
    }
    
    /// <summary>
    /// Toggle between detailed and compact view for this instance
    /// </summary>
    public void ToggleDetailedView()
    {
        showDetailedView = !showDetailedView;
    }
    
    /// <summary>
    /// Check if global UI is visible
    /// </summary>
    public static bool IsGlobalUIVisible => globalShowUI;
}
