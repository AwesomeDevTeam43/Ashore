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

    private GameObject player;
    private XP_System xpSystem;
    private HealthSystem healthSystem;

    private Player_Controller player_Controller;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        player_Controller = player.GetComponent<Player_Controller>();

        xpSystem = player.GetComponent<XP_System>();
        healthSystem = player.GetComponent<HealthSystem>();

        xpSystem.OnCollectXP += UpdateXPBar;
        xpSystem.OnLevelUp += UpdateLevel;

        healthSystem.OnHealthChanged += UpdateHPBar;

        UpdateXPBar(0);
        UpdateLevel(1);
    }

    private void Update()
    {
        if (player_Controller.CurrentEquipment != null)
        {
            equipment_1.enabled = true;
        }
        else
        {
            equipment_1.enabled = false;
        }
    }

    private void UpdateXPBar(int xpAmount)
    {
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
        _levelText.text = $"{newLevel}";
        UpdateXPBar(0);
    }

    private void UpdateHPBar(int health, int maxHealth)
    {
        float currentHP = (float)health;
        float maxHP = (float)maxHealth;

        if (maxHP > 0)
        {
            _hpBar.fillAmount = Mathf.Clamp01(currentHP / maxHP);
        }
        else
        {
            _hpBar.fillAmount = 0f;
        }

        Debug.Log($"HP Bar updated: {currentHP}/{maxHP} = {_hpBar.fillAmount}");
    }
}
