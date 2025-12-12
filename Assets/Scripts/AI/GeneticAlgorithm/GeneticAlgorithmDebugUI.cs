using UnityEngine;

/// <summary>
/// UI de debug para visualizar o estado do Algoritmo Genético em tempo real.
/// Mostra informações sobre a população, geração atual e melhor genoma.
/// </summary>
public class GeneticAlgorithmDebugUI : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.G;
    
    [Header("Position")]
    [SerializeField] private float xPosition = 10f;
    [SerializeField] private float yPosition = 10f;
    [SerializeField] private float width = 350f;
    
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle boxStyle;
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
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.3f, 0.9f, 0.3f) }
        };
        
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };
        
        valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.9f, 0.3f) }
        };
        
        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.7f)) }
        };
        
        stylesInitialized = true;
    }
    
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = color;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    
    private void OnGUI()
    {
        if (!showUI) return;
        
        InitStyles();
        
        var evolver = GeneticEnemyEvolver.Instance;
        if (evolver == null)
        {
            GUI.Box(new Rect(xPosition, yPosition, width, 60), "", boxStyle);
            GUI.Label(new Rect(xPosition + 10, yPosition + 10, width - 20, 40), 
                "🧬 Genetic Algorithm\nNo GeneticEnemyEvolver found!", headerStyle);
            return;
        }
        
        float height = 200f;
        var bestGenome = evolver.GetBestGenome();
        if (bestGenome != null) height += 120f;
        
        // Background box
        GUI.Box(new Rect(xPosition, yPosition, width, height), "", boxStyle);
        
        float y = yPosition + 10;
        float lineHeight = 20f;
        float labelWidth = 160f;
        float valueWidth = width - labelWidth - 30f;
        
        // Header
        GUI.Label(new Rect(xPosition + 10, y, width - 20, 25), "🧬 GENETIC ALGORITHM", headerStyle);
        y += 30f;
        
        // Divider
        GUI.Label(new Rect(xPosition + 10, y, width - 20, 2), "────────────────────────────────", labelStyle);
        y += 15f;
        
        // Stats
        DrawLabelValue(ref y, "Generation:", evolver.Generation.ToString(), labelWidth, valueWidth);
        DrawLabelValue(ref y, "Population Size:", evolver.PopulationSize.ToString(), labelWidth, valueWidth);
        DrawLabelValue(ref y, "Avg Fitness:", evolver.AveragePopulationFitness.ToString("F2"), labelWidth, valueWidth);
        
        y += 10f;
        GUI.Label(new Rect(xPosition + 10, y, width - 20, 2), "────────────────────────────────", labelStyle);
        y += 15f;
        
        // Best Genome
        if (bestGenome != null)
        {
            GUI.Label(new Rect(xPosition + 10, y, width - 20, 20), "🏆 BEST GENOME", headerStyle);
            y += 25f;
            
            DrawLabelValue(ref y, "Fitness:", bestGenome.AverageFitness.ToString("F2"), labelWidth, valueWidth);
            DrawLabelValue(ref y, "Times Used:", bestGenome.TimesUsed.ToString(), labelWidth, valueWidth);
            
            y += 5f;
            
            // Genes como barras visuais
            DrawGeneBar(ref y, "Health", bestGenome.healthGene);
            DrawGeneBar(ref y, "Damage", bestGenome.damageGene);
            DrawGeneBar(ref y, "Speed", bestGenome.movementSpeedGene);
            DrawGeneBar(ref y, "Aggression", bestGenome.aggressivenessGene);
        }
        
        // Instructions
        y += 10f;
        GUI.Label(new Rect(xPosition + 10, y, width - 20, 20), 
            $"Press [{toggleKey}] to toggle", labelStyle);
    }
    
    private void DrawLabelValue(ref float y, string label, string value, float labelWidth, float valueWidth)
    {
        GUI.Label(new Rect(xPosition + 10, y, labelWidth, 20), label, labelStyle);
        GUI.Label(new Rect(xPosition + 10 + labelWidth, y, valueWidth, 20), value, valueStyle);
        y += 20f;
    }
    
    private void DrawGeneBar(ref float y, string geneName, float geneValue)
    {
        float barX = xPosition + 100;
        float barWidth = width - 120;
        float barHeight = 12f;
        
        // Label
        GUI.Label(new Rect(xPosition + 10, y, 90, 20), geneName, labelStyle);
        
        // Background bar
        GUI.DrawTexture(new Rect(barX, y + 3, barWidth, barHeight), 
            MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.2f)));
        
        // Filled bar
        Color barColor = Color.Lerp(Color.green, Color.red, geneValue);
        GUI.DrawTexture(new Rect(barX, y + 3, barWidth * geneValue, barHeight), 
            MakeTexture(1, 1, barColor));
        
        // Value text
        GUI.Label(new Rect(barX + barWidth + 5, y, 40, 20), 
            $"{geneValue:F2}", labelStyle);
        
        y += 18f;
    }
}
