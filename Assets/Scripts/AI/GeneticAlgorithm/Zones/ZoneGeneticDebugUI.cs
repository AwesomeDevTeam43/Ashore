using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI de debug para visualizar o estado das zonas genéticas.
/// Mostra população, geração e fitness de cada zona.
/// </summary>
public class ZoneGeneticDebugUI : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.Z;
    
    [Header("Position")]
    [SerializeField] private float xPosition = 10f;
    [SerializeField] private float yPosition = 150f;
    
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle zoneStyle;
    private GUIStyle currentZoneStyle;
    private Texture2D backgroundTex;
    private bool stylesInitialized = false;
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showUI = !showUI;
        }
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        backgroundTex = MakeTexture(2, 2, new Color(0, 0, 0, 0.75f));
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.4f, 0.8f, 1f) }
        };
        
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };
        
        zoneStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
            normal = { textColor = Color.white, background = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.9f)) },
            padding = new RectOffset(5, 5, 3, 3),
            margin = new RectOffset(2, 2, 2, 2)
        };
        
        currentZoneStyle = new GUIStyle(zoneStyle)
        {
            normal = { textColor = Color.yellow, background = MakeTexture(2, 2, new Color(0.3f, 0.4f, 0.2f, 0.9f)) }
        };
        
        stylesInitialized = true;
    }
    
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
    
    private void OnGUI()
    {
        if (!showUI) return;
        
        var manager = ZoneGeneticManager.Instance;
        if (manager == null)
        {
            GUI.Box(new Rect(xPosition, yPosition, 200, 40), "");
            GUI.Label(new Rect(xPosition + 10, yPosition + 10, 180, 25), 
                "🗺️ ZoneGeneticManager not found", headerStyle);
            return;
        }
        
        InitStyles();
        
        var zones = manager.GetAllZones();
        var currentZone = manager.GetCurrentZone();
        
        float panelWidth = 320f;
        float panelHeight = 60f + (zones.Count * 55f);
        
        // Background
        GUI.DrawTexture(new Rect(xPosition, yPosition, panelWidth, panelHeight), backgroundTex);
        
        float y = yPosition + 10;
        
        // Header
        GUI.Label(new Rect(xPosition + 10, y, panelWidth - 20, 25), 
            "🗺️ ZONE GENETIC SYSTEM", headerStyle);
        y += 25;
        
        GUI.Label(new Rect(xPosition + 10, y, panelWidth - 20, 20), 
            $"Current: {(currentZone != null ? currentZone.displayName : "None")}", labelStyle);
        y += 25;
        
        // Zonas
        foreach (var zone in zones)
        {
            if (zone == null) continue;
            
            var stats = manager.GetZoneStats(zone.zoneId);
            bool isCurrent = currentZone != null && currentZone.zoneId == zone.zoneId;
            
            // Cor baseada na dificuldade
            float diff = (zone.baseHealthMultiplier + zone.baseDamageMultiplier) / 2f;
            Color diffColor = Color.Lerp(Color.green, Color.red, (diff - 0.5f) / 2f);
            
            string zoneText = $"{zone.displayName} (Diff: {diff:F1}x)\n" +
                             $"Gen: {stats.generation} | Pop: {stats.populationSize} | Fit: {stats.averageFitness:F1}\n" +
                             $"Deaths: {stats.deathsSinceEvolution}/{zone.evolveTriggerCount}";
            
            GUIStyle style = isCurrent ? currentZoneStyle : zoneStyle;
            GUI.Box(new Rect(xPosition + 5, y, panelWidth - 10, 50), zoneText, style);
            
            // Indicador de dificuldade
            GUI.DrawTexture(new Rect(xPosition + panelWidth - 20, y + 5, 10, 40), 
                MakeTexture(1, 1, diffColor));
            
            y += 55;
        }
        
        // Instruções
        GUI.Label(new Rect(xPosition + 10, y, panelWidth - 20, 20), 
            $"[{toggleKey}] Toggle UI", labelStyle);
    }
}
