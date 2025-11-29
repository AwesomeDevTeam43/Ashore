using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Manage_UI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _xpBar;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Image _hpBar;
    [SerializeField] private Image equipment_1;
    [Space]
    [SerializeField] private Image mainWeaponHudIcon;
    [SerializeField] private Sprite meleeIcon;
    [SerializeField] private Sprite rangedIcon;

    private GameObject player;
    private XP_System xpSystem;
    private HealthSystem healthSystem;
    private Player_Controller player_Controller;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        player_Controller = player != null ? player.GetComponent<Player_Controller>() : null;
        xpSystem = player != null ? player.GetComponent<XP_System>() : null;
        healthSystem = player != null ? player.GetComponent<HealthSystem>() : null;
    }

    void Start()
    {
        if (xpSystem != null)
        {
            xpSystem.OnCollectXP += UpdateXPBar;
            xpSystem.OnLevelUp += UpdateLevel;
            UpdateLevel(xpSystem.CurrentLevel);
            UpdateXPBar(0);
        }
        
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += UpdateHPBar;
            UpdateHPBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
        }
    }

    private void UpdateAllUI()
    {
        Debug.Log("Manage_UI: UpdateAllUI chamado - Atualizando referências");
        
        // Unsubscribe old references
        UnsubscribeFromEvents();
        
        // Re-acquire references
        InitializeReferences();
        
        // Resubscribe
        SubscribeToEvents();
        
        // Refresh UI
        RefreshAllUI();
    }

    private void Update()
    {
        if (player_Controller == null) return;
        
        if (player_Controller.CurrentEquipment != null)
        {
            equipment_1.enabled = true;
        }
        else
        {
            equipment_1.enabled = false;
        }

        if (mainWeaponHudIcon != null)
        {
            mainWeaponHudIcon.enabled = true;
            mainWeaponHudIcon.sprite = (player_Controller.CurrentMainWeapon == Player_Controller.MainWeaponType.Melee)
                ? meleeIcon
                : rangedIcon;
        }
    }

    private void UpdateXPBar(int xpAmount)
    {
        if (_xpBar == null || xpSystem == null) return;
        
        float currentXp = xpSystem.CurrentXp;
        float maxXp = xpSystem.MaxXpPerLevel;

        if (maxXp > 0)
        {
            _xpBar.fillAmount = Mathf.Clamp01(currentXp / maxXp);
        }
        else
        {
            _xpBar.fillAmount = 0f;
        }
    }

    private void UpdateLevel(int newLevel)
    {
        if (_levelText != null)
            _levelText.text = $"{newLevel}";
        UpdateXPBar(0);
    }

    private void UpdateHPBar(int health, int maxHealth)
    {
        if (_hpBar == null)
        {
            Debug.LogWarning("Manage_UI: HP Bar Image não está atribuída!");
            return;
        }
        
        float currentHP = (float)health;
        float maxHP = (float)maxHealth;

        if (maxHP > 0)
        {
            float fillValue = Mathf.Clamp01(currentHP / maxHP);
            _hpBar.fillAmount = fillValue;
            Debug.Log($"Manage_UI: HP Bar updated: {currentHP}/{maxHP} = {fillValue} (fillAmount: {_hpBar.fillAmount})");
        }
        else
        {
            _hpBar.fillAmount = 0f;
            Debug.LogWarning("Manage_UI: Max HP é 0!");
        }
    }

    public void RebindAndRefresh()
    {
        UpdateAllUI();
    }
}
