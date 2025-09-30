using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Manage_UI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _xpBar;
    [SerializeField] private TextMeshProUGUI _levelText;

    [Header("Systems References")]
    [SerializeField] private XP_System xpSystem;
    //[SerializeField] private HealthSystem healthSystem;

    void Start()
    {
        xpSystem.OnCollectXP += UpdateXPBar;
        xpSystem.OnLevelUp += UpdateLevel;

        UpdateXPBar(0);
        UpdateLevel(1);
    }

    private void UpdateXPBar(int xpAmount)
    {
        float currentXp = xpSystem.CurrentXp;
        float maxXp = xpSystem.MaxXpPerLevel;

        _xpBar.fillAmount = currentXp / maxXp;
    }

    private void UpdateLevel(int newLevel)
    {
        _levelText.text = $"{newLevel}";
        UpdateXPBar(0);

    }
}
