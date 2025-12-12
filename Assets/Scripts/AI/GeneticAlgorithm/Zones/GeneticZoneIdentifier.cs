using UnityEngine;

/// <summary>
/// Componente que identifica a zona genética de uma área.
/// Adiciona a um Trigger/Collider para mudar a zona quando o jogador entra.
/// 
/// Também pode ser usado em SpawnPoints para definir de que zona os inimigos vêm.
/// </summary>
public class GeneticZoneIdentifier : MonoBehaviour
{
    [Header("Zone Configuration")]
    [Tooltip("Configuração da zona")]
    [SerializeField] private GeneticZoneConfig zoneConfig;
    
    [Header("Auto-Detection")]
    [Tooltip("Se true, muda a zona automaticamente quando o jogador entra no trigger")]
    [SerializeField] private bool autoDetectPlayer = true;
    
    [Tooltip("Tag do jogador")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Visual Debug")]
    [SerializeField] private bool showZoneGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.2f);
    
    /// <summary>
    /// Retorna a configuração da zona
    /// </summary>
    public GeneticZoneConfig ZoneConfig => zoneConfig;
    
    /// <summary>
    /// Retorna o ID da zona
    /// </summary>
    public string ZoneId => zoneConfig != null ? zoneConfig.zoneId : "default";
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoDetectPlayer) return;
        if (!other.CompareTag(playerTag)) return;
        
        if (ZoneGeneticManager.Instance != null && zoneConfig != null)
        {
            ZoneGeneticManager.Instance.SetCurrentZone(zoneConfig);
            Debug.Log($"🗺️ Player entered zone: {zoneConfig.displayName}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!autoDetectPlayer) return;
        if (!other.CompareTag(playerTag)) return;
        
        if (ZoneGeneticManager.Instance != null && zoneConfig != null)
        {
            ZoneGeneticManager.Instance.SetCurrentZone(zoneConfig);
            Debug.Log($"🗺️ Player entered zone: {zoneConfig.displayName}");
        }
    }
    
    /// <summary>
    /// Obtém um genoma desta zona específica
    /// </summary>
    public EnemyGenome GetGenomeForThisZone()
    {
        if (ZoneGeneticManager.Instance != null && zoneConfig != null)
        {
            return ZoneGeneticManager.Instance.GetGenomeForZone(zoneConfig.zoneId);
        }
        
        // Fallback: usa o sistema global
        if (GeneticEnemyEvolver.Instance != null)
        {
            return GeneticEnemyEvolver.Instance.GetGenomeForNewEnemy(GetInstanceID());
        }
        
        return EnemyGenome.CreateRandom();
    }
    
    private void OnDrawGizmos()
    {
        if (!showZoneGizmo || zoneConfig == null) return;
        
        // Cor baseada na dificuldade
        Color color = gizmoColor;
        float difficulty = (zoneConfig.baseHealthMultiplier + zoneConfig.baseDamageMultiplier) / 2f;
        color = Color.Lerp(Color.green, Color.red, (difficulty - 0.5f) / 2.5f);
        color.a = 0.2f;
        
        Gizmos.color = color;
        
        // Tenta desenhar baseado no collider
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.8f);
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
        
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
        
        // Label
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
            $"🗺️ {zoneConfig.displayName}\nDiff: {difficulty:F1}x");
#endif
    }
}
