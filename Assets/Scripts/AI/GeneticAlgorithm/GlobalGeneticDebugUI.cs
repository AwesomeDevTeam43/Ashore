using UnityEngine;

/// <summary>
/// UI de debug para o sistema de Algoritmo Genético GLOBAL.
/// Mostra geração, kills, fitness e progressão.
/// </summary>
public class GlobalGeneticDebugUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.G;
    
    [Header("Position")]
    [SerializeField] private float xPos = 10f;
    [SerializeField] private float yPos = 10f;
    
    private GUIStyle boxStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private Texture2D bgTex;
    private Texture2D barBgTex;
    private bool initialized = false;
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showUI = !showUI;
        }
    }
    
    private void InitStyles()
    {
        if (initialized) return;
        
        bgTex = MakeTex(new Color(0, 0, 0, 0.85f));
        barBgTex = MakeTex(new Color(0.2f, 0.2f, 0.2f, 1f));
        
        boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = bgTex } };
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.4f, 1f, 0.4f) },
            alignment = TextAnchor.MiddleCenter
        };
        
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };
        
        valueStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
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
            GUI.Box(new Rect(xPos, yPos, 250, 50), "");
            GUI.Label(new Rect(xPos + 10, yPos + 15, 230, 25), "🧬 GlobalGeneticEvolver not found!");
            return;
        }
        
        InitStyles();
        
        float width = 280f;
        float height = 220f;
        
        // Background
        GUI.Box(new Rect(xPos, yPos, width, height), "", boxStyle);
        
        float y = yPos + 10;
        
        // Header
        GUI.Label(new Rect(xPos, y, width, 25), "🧬 GLOBAL GENETIC ALGORITHM", headerStyle);
        y += 30;
        
        // Stats
        DrawStat(xPos + 15, ref y, "Generation:", ga.Generation.ToString());
        DrawStat(xPos + 15, ref y, "Total Kills:", ga.TotalKills.ToString());
        DrawStat(xPos + 15, ref y, "Avg Fitness:", ga.AverageFitness.ToString("F2"));
        DrawStat(xPos + 15, ref y, "Difficulty:", $"×{ga.CurrentDifficultyMultiplier:F2}");
        
        y += 10;
        
        // Progress bar para próxima evolução
        GUI.Label(new Rect(xPos + 15, y, 100, 20), "Next Evolution:", labelStyle);
        y += 18;
        
        // Barra de progresso
        float progress = 0f; // Não temos acesso direto, mostramos só o texto
        GUI.DrawTexture(new Rect(xPos + 15, y, width - 30, 15), barBgTex);
        GUI.DrawTexture(new Rect(xPos + 15, y, (width - 30) * progress, 15), MakeTex(Color.green));
        y += 25;
        
        // Best Genome
        var best = ga.GetBestGenome();
        if (best != null)
        {
            GUI.Label(new Rect(xPos + 15, y, width - 30, 20), "🏆 Best Genome:", labelStyle);
            y += 18;
            
            DrawGeneBar(xPos + 15, ref y, "HP", best.healthGene, Color.red, width - 30);
            DrawGeneBar(xPos + 15, ref y, "DMG", best.damageGene, Color.blue, width - 30);
            DrawGeneBar(xPos + 15, ref y, "SPD", best.movementSpeedGene, Color.cyan, width - 30);
            DrawGeneBar(xPos + 15, ref y, "AGR", best.aggressivenessGene, Color.magenta, width - 30);
        }
        
        // Instructions
        GUI.Label(new Rect(xPos + 15, height + yPos - 25, width - 30, 20), 
            $"[{toggleKey}] Toggle", labelStyle);
    }
    
    private void DrawStat(float x, ref float y, string label, string value)
    {
        GUI.Label(new Rect(x, y, 120, 20), label, labelStyle);
        GUI.Label(new Rect(x + 120, y, 100, 20), value, valueStyle);
        y += 20;
    }
    
    private void DrawGeneBar(float x, ref float y, string label, float value, Color color, float maxWidth)
    {
        float labelW = 35;
        float barW = maxWidth - labelW - 40;
        
        GUI.Label(new Rect(x, y, labelW, 16), label, labelStyle);
        GUI.DrawTexture(new Rect(x + labelW, y + 2, barW, 10), barBgTex);
        GUI.DrawTexture(new Rect(x + labelW, y + 2, barW * value, 10), MakeTex(color));
        GUI.Label(new Rect(x + labelW + barW + 5, y, 35, 16), value.ToString("F2"), labelStyle);
        
        y += 16;
    }
}
