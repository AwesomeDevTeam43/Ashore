using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentPage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button disableButton;

    private Player_Controller player;

    private void Awake()
    {
        if (disableButton != null)
        {
            disableButton.onClick.RemoveAllListeners();
            disableButton.onClick.AddListener(OnDisablePressed);
            // Ensure visual highlight on hover/selection
            var hi = disableButton.GetComponent<SelectionHighlight>();
            if (hi == null) disableButton.gameObject.AddComponent<SelectionHighlight>();
        }
    }

    private void OnEnable()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.GetComponent<Player_Controller>();
            if (player == null) player = Object.FindFirstObjectByType<Player_Controller>();
        }
        Refresh();
    }

    public void Refresh()
    {
        if (player == null)
        {
            SetNone();
            return;
        }

        var eq = player.CurrentEquipment;
        if (eq != null && eq.equipmentData != null)
        {
            if (icon != null) icon.sprite = eq.equipmentData.icon;
            if (nameText != null) nameText.text = eq.equipmentData.itemName;
            if (descriptionText != null) descriptionText.text = eq.equipmentData.description;
            if (disableButton != null)
            {
                disableButton.gameObject.SetActive(true);
                var label = disableButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "Disable";
                disableButton.interactable = true;
            }
        }
        else
        {
            SetNone();
        }
    }

    private void SetNone()
    {
        if (icon != null) icon.sprite = null;
        if (nameText != null) nameText.text = "None";
        if (descriptionText != null) descriptionText.text = "";
        if (disableButton != null)
        {
            var label = disableButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = "Disable";
            disableButton.interactable = false;
        }
    }

    private void OnDisablePressed()
    {
        if (player == null) return;
        var eq = player.CurrentEquipment;
        if (eq != null)
        {
            // Unequip and clear from player
            eq.Unequip();
            eq.isEquipped = false;
            player.SetCurrentEquipment(null);

            // Return the equipment data back to inventory (1x)
            if (eq.equipmentData != null && Inventory.instance != null)
            {
                Inventory.instance.Add(eq.equipmentData, 1);
            }
        }
        Refresh();
    }
}