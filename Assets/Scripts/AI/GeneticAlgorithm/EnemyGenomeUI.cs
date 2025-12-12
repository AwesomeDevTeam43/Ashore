using UnityEngine;

/// <summary>
/// UI flutuante que mostra as stats genéticas por cima do inimigo.
/// Adiciona automaticamente ao inimigo junto com o EnemyFitnessTracker.
/// </summary>
public class EnemyGenomeUI : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [SerializeField] private float maxDistance = 20f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color geneBarColor = Color.cyan;
    [SerializeField] private int fontSize = 10;
    
    private EnemyFitnessTracker fitnessTracker;
    private Camera mainCamera;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private Texture2D backgroundTexture;
    private Texture2D barBackgroundTexture;
    private Texture2D barFillTexture;
    private bool stylesInitialized = false;
    
    private void Awake()
    {
        fitnessTracker = GetComponent<EnemyFitnessTracker>();
        mainCamera = Camera.main;
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
        
        // Cria texturas
        backgroundTexture = MakeTexture(2, 2, backgroundColor);
        barBackgroundTexture = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.9f));
        barFillTexture = MakeTexture(2, 2, geneBarColor);
        
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = backgroundTexture;
        boxStyle.padding = new RectOffset(5, 5, 5, 5);
        
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;
        labelStyle.normal.textColor = textColor;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        
        headerStyle = new GUIStyle(labelStyle);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
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
        if (!showUI || fitnessTracker == null || fitnessTracker.Genome == null) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        // Verifica distância da câmara
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        if (distance > maxDistance) return;
        
        // Converte posição world para screen
        Vector3 worldPos = transform.position + offset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        // Verifica se está atrás da câmara
        if (screenPos.z < 0) return;
        
        InitStyles();
        
        // Converte para coordenadas GUI (Y invertido)
        float guiY = Screen.height - screenPos.y;
        
        // Dimensões do painel
        float panelWidth = 140;
        float panelHeight = 130;
        float x = screenPos.x - panelWidth / 2;
        float y = guiY - panelHeight;
        
        // Garante que fica dentro do ecrã
        x = Mathf.Clamp(x, 5, Screen.width - panelWidth - 5);
        y = Mathf.Clamp(y, 5, Screen.height - panelHeight - 5);
        
        // Desenha o painel
        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "", boxStyle);
        
        var genome = fitnessTracker.Genome;
        float lineY = y + 5;
        float lineHeight = 14;
        float barHeight = 8;
        float labelWidth = 50;
        float barWidth = panelWidth - labelWidth - 25;
        
        // Header
        GUI.Label(new Rect(x, lineY, panelWidth, lineHeight + 2), "🧬 GENOME", headerStyle);
        lineY += lineHeight + 4;
        
        // Genes como barras
        DrawGeneBar(x + 5, ref lineY, "HP", genome.healthGene, labelWidth, barWidth, barHeight, Color.red);
        DrawGeneBar(x + 5, ref lineY, "DMG", genome.damageGene, labelWidth, barWidth, barHeight, Color.blue);
        DrawGeneBar(x + 5, ref lineY, "SPD", genome.movementSpeedGene, labelWidth, barWidth, barHeight, Color.cyan);
        DrawGeneBar(x + 5, ref lineY, "ATK", genome.attackSpeedGene, labelWidth, barWidth, barHeight, Color.yellow);
        DrawGeneBar(x + 5, ref lineY, "AGR", genome.aggressivenessGene, labelWidth, barWidth, barHeight, Color.magenta);
        DrawGeneBar(x + 5, ref lineY, "RES", (genome.meleeResistanceGene + genome.rangedResistanceGene) / 2f, labelWidth, barWidth, barHeight, Color.green);
        
        // Fitness atual
        lineY += 2;
        GUI.Label(new Rect(x + 5, lineY, panelWidth - 10, lineHeight), 
            $"Fitness: {fitnessTracker.DamageDealtToPlayer:F1}", labelStyle);
    }
    
    private void DrawGeneBar(float x, ref float y, string label, float value, float labelWidth, float barWidth, float barHeight, Color barColor)
    {
        // Label
        GUI.Label(new Rect(x, y, labelWidth, 14), label, labelStyle);
        
        // Background bar
        GUI.DrawTexture(new Rect(x + labelWidth, y + 3, barWidth, barHeight), barBackgroundTexture);
        
        // Filled bar
        Texture2D fillTex = MakeTexture(2, 2, barColor);
        GUI.DrawTexture(new Rect(x + labelWidth, y + 3, barWidth * value, barHeight), fillTex);
        
        // Value text
        GUI.Label(new Rect(x + labelWidth + barWidth + 2, y, 25, 14), $"{value:F1}", labelStyle);
        
        y += 14;
    }
    
    private void OnDestroy()
    {
        // Limpa texturas
        if (backgroundTexture != null) Destroy(backgroundTexture);
        if (barBackgroundTexture != null) Destroy(barBackgroundTexture);
        if (barFillTexture != null) Destroy(barFillTexture);
    }
    
    /// <summary>
    /// Toggle da UI
    /// </summary>
    public void ToggleUI()
    {
        showUI = !showUI;
    }
    
    /// <summary>
    /// Define se a UI está visível
    /// </summary>
    public void SetVisible(bool visible)
    {
        showUI = visible;
    }
}
