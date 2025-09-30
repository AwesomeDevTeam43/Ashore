using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Manage_UI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _xpBar;
    [SerializeField] private TextMeshProUGUI _levelText;

    [Header("Systems References")]
   // [SerializeField] private XP_System xpSystem;
    //[SerializeField] private HealthSystem healthSystem;
    //
    private GameObject player;
    private XP_System xpSystem;
    private HealthSystem healthSystem;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        xpSystem = player.GetComponent<XP_System>();
        healthSystem = player.GetComponent<HealthSystem>();

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
