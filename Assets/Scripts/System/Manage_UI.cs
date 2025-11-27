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

    void Start()
    {
        InitializeReferences();
        SubscribeToEvents();
        RefreshAllUI();
    }

    private void OnEnable()
    {
        Player_Controller.OnPlayerLoad += UpdateAllUI;
    }

    private void OnDisable()
    {
        Player_Controller.OnPlayerLoad -= UpdateAllUI;
        UnsubscribeFromEvents();
    }

    private void InitializeReferences()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            Debug.LogError("Manage_UI: Player não encontrado!");
            return;
        }

        player_Controller = player.GetComponent<Player_Controller>();
        xpSystem = player.GetComponent<XP_System>();
        healthSystem = player.GetComponent<HealthSystem>();

        if (xpSystem == null)
            Debug.LogError("Manage_UI: XP_System não encontrado no player!");
        
        if (healthSystem == null)
            Debug.LogError("Manage_UI: HealthSystem não encontrado no player!");
    }

    private void SubscribeToEvents()
    {
        if (xpSystem != null)
        {
            xpSystem.OnCollectXP += UpdateXPBar;
            xpSystem.OnLevelUp += UpdateLevel;
            Debug.Log("Manage_UI: Subscrito aos eventos de XP");
        }

        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += UpdateHPBar;
            Debug.Log("Manage_UI: Subscrito aos eventos de Health");
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (xpSystem != null)
        {
            xpSystem.OnCollectXP -= UpdateXPBar;
            xpSystem.OnLevelUp -= UpdateLevel;
        }

        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHPBar;
        }
    }

    private void RefreshAllUI()
    {
        if (xpSystem != null)
        {
            UpdateLevel(xpSystem.CurrentLevel);
            UpdateXPBar(0);
        }
        
        if (healthSystem != null)
        {
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
