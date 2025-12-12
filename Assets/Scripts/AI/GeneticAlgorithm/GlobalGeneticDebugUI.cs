using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Debug UI for the Global Genetic Algorithm system.
/// Shows species-based evolution stats, difficulty modifier, and detailed metrics.
/// 
/// Press G (configurable) to toggle the UI.
/// </summary>
public class GlobalGeneticDebugUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.G;
    [SerializeField] private bool showDetailedStats = false;
    
    [Header("Position")]
    [SerializeField] private float xPos = 10f;
    [SerializeField] private float yPos = 10f;
    
    // GUI styles
    private GUIStyle boxStyle;
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle speciesStyle;
    private Texture2D bgTex;
    private Texture2D barBgTex;
    private Texture2D barFillTex;
    private bool initialized = false;
    
    // Scroll position for species list
    private Vector2 scrollPos;
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showUI = !showUI;
        }
        
        // Tab to toggle detailed stats
        if (showUI && Input.GetKeyDown(KeyCode.Tab))
        {
            showDetailedStats = !showDetailedStats;
        }
    }
    
    private void InitStyles()
    {
        if (initialized) return;
        
        bgTex = MakeTex(new Color(0, 0, 0, 0.9f));
        barBgTex = MakeTex(new Color(0.2f, 0.2f, 0.2f, 1f));
        barFillTex = MakeTex(new Color(0.3f, 0.8f, 0.3f, 1f));
        
        boxStyle = new GUIStyle(GUI.skin.box) 
        { 
            normal = { background = bgTex },
            padding = new RectOffset(5, 5, 5, 5)
        };
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.4f, 1f, 0.4f) },
            alignment = TextAnchor.MiddleCenter
        };
        
        subHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.8f, 0.8f, 1f) }
        };
        
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };
        
        valueStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow },
            alignment = TextAnchor.MiddleRight
        };
        
        speciesStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.7f, 0.9f, 1f) }
        };
        
        initialized = true;
    }
    
    private Texture2D MakeTex(Color color)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { color, color, color, color });
        tex.Apply();
        return tex;
    }
    
    private void OnGUI()
    {
        if (!showUI) return;
        
        var ga = GlobalGeneticEvolver.Instance;
        if (ga == null)
        {
            GUI.Box(new Rect(xPos, yPos, 280, 50), "", boxStyle);
            GUI.Label(new Rect(xPos + 10, yPos + 15, 260, 25), "🧬 GlobalGeneticEvolver not found!");
            return;
        }
        
        InitStyles();
        
        float width = 320f;
        float baseHeight = 180f;
        
        // Get species stats
        var speciesStats = ga.GetAllSpeciesStats();
        float speciesHeight = showDetailedStats ? Mathf.Min(speciesStats.Count * 60f, 240f) : speciesStats.Count * 22f;
        float height = baseHeight + speciesHeight + 40f;
        
        // Background
        GUI.Box(new Rect(xPos, yPos, width, height), "", boxStyle);
        
        float y = yPos + 10;
        
        // Header
        GUI.Label(new Rect(xPos, y, width, 25), "🧬 GENETIC ALGORITHM", headerStyle);
        y += 25;
        
        // Difficulty bar
        float diffMod = ga.CurrentDifficultyModifier;
        DrawDifficultyBar(xPos + 15, y, width - 30, diffMod);
        y += 30;
        
        // Global stats
        DrawStat(xPos + 15, ref y, "Total Kills:", ga.TotalKills.ToString());
        DrawStat(xPos + 15, ref y, "Player Deaths:", ga.PlayerDeaths.ToString());
        DrawStat(xPos + 15, ref y, "Species Tracked:", speciesStats.Count.ToString());
        
        y += 10;
        
        // Species section header
        GUI.Label(new Rect(xPos + 15, y, width - 30, 20), "── Species Evolution ──", subHeaderStyle);
        y += 22;
        
        GUI.Label(new Rect(xPos + width - 100, y - 20, 80, 16), "(Tab: details)", new GUIStyle(labelStyle) 
        { 
            fontSize = 9, 
            normal = { textColor = Color.gray },
            alignment = TextAnchor.MiddleRight
        });
        
        // Species list
        if (speciesStats.Count > 0)
        {
            foreach (var kvp in speciesStats)
            {
                string speciesName = EnemySpeciesHelper.GetDisplayName(kvp.Key);
                int gen = kvp.Value.generation;
                float fitness = kvp.Value.avgFitness;
                
                if (showDetailedStats)
                {
                    // Detailed view
                    GUI.Label(new Rect(xPos + 20, y, 150, 18), $"▸ {speciesName}", speciesStyle);
                    y += 18;
                    DrawStat(xPos + 35, ref y, "Generation:", gen.ToString(), 100, 70);
                    DrawStat(xPos + 35, ref y, "Avg Fitness:", fitness.ToString("F1"), 100, 70);
                    DrawStat(xPos + 35, ref y, "Pop Size:", kvp.Value.popSize.ToString(), 100, 70);
                    y += 5;
                }
                else
                {
                    // Compact view
                    GUI.Label(new Rect(xPos + 20, y, 120, 18), $"▸ {speciesName}", speciesStyle);
                    GUI.Label(new Rect(xPos + 150, y, 60, 18), $"Gen {gen}", valueStyle);
                    GUI.Label(new Rect(xPos + 220, y, 70, 18), $"Fit:{fitness:F0}", valueStyle);
                    y += 20;
                }
            }
        }
        else
        {
            GUI.Label(new Rect(xPos + 20, y, width - 40, 18), "No species evolved yet", labelStyle);
            y += 20;
        }
        
        y += 10;
        
        // Footer hint
        GUI.Label(new Rect(xPos + 15, y, width - 30, 16), $"Press [{toggleKey}] to hide", new GUIStyle(labelStyle) 
        { 
            fontSize = 9, 
            normal = { textColor = Color.gray },
            alignment = TextAnchor.MiddleCenter
        });
    }
    
    private void DrawStat(float x, ref float y, string label, string value, float labelWidth = 140, float valueWidth = 100)
    {
        GUI.Label(new Rect(x, y, labelWidth, 18), label, labelStyle);
        GUI.Label(new Rect(x + labelWidth, y, valueWidth, 18), value, valueStyle);
        y += 18;
    }
    
    private void DrawDifficultyBar(float x, float y, float width, float value)
    {
        // Label
        GUI.Label(new Rect(x, y, 100, 16), "Difficulty:", labelStyle);
        
        // Bar background
        float barX = x + 75;
        float barWidth = width - 120;
        float barHeight = 14;
        GUI.DrawTexture(new Rect(barX, y + 1, barWidth, barHeight), barBgTex);
        
        // Calculate fill (0.5 to 2.5 range, mapped to 0-1)
        float normalizedValue = Mathf.Clamp01((value - 0.5f) / 2f);
        
        // Choose color based on difficulty
        Color barColor;
        if (value < 0.8f)
            barColor = new Color(0.3f, 0.7f, 1f); // Blue (easy)
        else if (value < 1.2f)
            barColor = new Color(0.3f, 0.9f, 0.3f); // Green (normal)
        else if (value < 1.8f)
            barColor = new Color(1f, 0.8f, 0.2f); // Yellow (hard)
        else
            barColor = new Color(1f, 0.3f, 0.3f); // Red (very hard)
        
        Texture2D fillTex = MakeTex(barColor);
        GUI.DrawTexture(new Rect(barX + 1, y + 2, (barWidth - 2) * normalizedValue, barHeight - 2), fillTex);
        
        // Value text
        GUI.Label(new Rect(x + width - 45, y, 45, 16), $"×{value:F2}", valueStyle);
    }
    
    /// <summary>
    /// Toggle UI visibility
    /// </summary>
    public void ToggleVisibility()
    {
        showUI = !showUI;
    }
    
    /// <summary>
    /// Set UI visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        showUI = visible;
    }
    
    /// <summary>
    /// Check if UI is visible
    /// </summary>
    public bool IsVisible => showUI;
}
