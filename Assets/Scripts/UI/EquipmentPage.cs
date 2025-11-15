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
    [Space]
    [SerializeField] private TextMeshProUGUI mainWeaponText; // Optional: shows current main weapon selection
    [SerializeField] private Button setMeleeButton;          // Optional: sets main weapon to Melee
    [SerializeField] private Button setRangedButton;         // Optional: sets main weapon to Ranged
    [Space]
    [SerializeField] private Button toggleMainWeaponButton;  // Optional: single toggle button for Melee<->Ranged
    [SerializeField] private Image mainWeaponIconUI;         // Optional: small icon in the equipment page
    [SerializeField] private Sprite meleeIcon;
    [SerializeField] private Sprite rangedIcon;

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
            // Ensure it's a navigable target
            if (disableButton.GetComponent<UINavTarget>() == null) disableButton.gameObject.AddComponent<UINavTarget>();
        }

        if (setMeleeButton != null)
        {
            setMeleeButton.onClick.RemoveAllListeners();
            setMeleeButton.onClick.AddListener(() => SetMainWeapon(Player_Controller.MainWeaponType.Melee));
            var hi = setMeleeButton.GetComponent<SelectionHighlight>();
            if (hi == null) setMeleeButton.gameObject.AddComponent<SelectionHighlight>();
            if (setMeleeButton.GetComponent<UINavTarget>() == null) setMeleeButton.gameObject.AddComponent<UINavTarget>();
        }
        if (setRangedButton != null)
        {
            setRangedButton.onClick.RemoveAllListeners();
            setRangedButton.onClick.AddListener(() => SetMainWeapon(Player_Controller.MainWeaponType.Ranged));
            var hi = setRangedButton.GetComponent<SelectionHighlight>();
            if (hi == null) setRangedButton.gameObject.AddComponent<SelectionHighlight>();
            if (setRangedButton.GetComponent<UINavTarget>() == null) setRangedButton.gameObject.AddComponent<UINavTarget>();
        }

        if (toggleMainWeaponButton != null)
        {
            toggleMainWeaponButton.onClick.RemoveAllListeners();
            toggleMainWeaponButton.onClick.AddListener(ToggleMainWeapon);
            var hi = toggleMainWeaponButton.GetComponent<SelectionHighlight>();
            if (hi == null) toggleMainWeaponButton.gameObject.AddComponent<SelectionHighlight>();
            if (toggleMainWeaponButton.GetComponent<UINavTarget>() == null) toggleMainWeaponButton.gameObject.AddComponent<UINavTarget>();
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

        // Update main weapon UI (optional)
        if (mainWeaponText != null)
        {
            mainWeaponText.text = $"Main Weapon: {player.CurrentMainWeapon}";
        }
        if (setMeleeButton != null) setMeleeButton.interactable = true;
        if (setRangedButton != null) setRangedButton.interactable = true;
        if (toggleMainWeaponButton != null)
        {
            var label = toggleMainWeaponButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = player.CurrentMainWeapon == Player_Controller.MainWeaponType.Melee ? "Switch to Ranged" : "Switch to Melee";
            }
        }
        if (mainWeaponIconUI != null)
        {
            mainWeaponIconUI.enabled = true;
            mainWeaponIconUI.sprite = player.CurrentMainWeapon == Player_Controller.MainWeaponType.Melee ? meleeIcon : rangedIcon;
        }

        RebuildNavScope();
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

    private void SetMainWeapon(Player_Controller.MainWeaponType type)
    {
        if (player == null) return;
        player.SetMainWeapon(type);
        Refresh();
    }

    private void ToggleMainWeapon()
    {
        if (player == null) return;
        var next = player.CurrentMainWeapon == Player_Controller.MainWeaponType.Melee
            ? Player_Controller.MainWeaponType.Ranged
            : Player_Controller.MainWeaponType.Melee;
        player.SetMainWeapon(next);
        Refresh();
    }

    // Return top-most selectable button for focus/navigation mapping
    public GameObject GetFirstSelectable()
    {
        if (toggleMainWeaponButton != null && toggleMainWeaponButton.gameObject.activeInHierarchy && toggleMainWeaponButton.interactable)
            return toggleMainWeaponButton.gameObject;
        if (setMeleeButton != null && setMeleeButton.gameObject.activeInHierarchy && setMeleeButton.interactable)
            return setMeleeButton.gameObject;
        if (setRangedButton != null && setRangedButton.gameObject.activeInHierarchy && setRangedButton.interactable)
            return setRangedButton.gameObject;
        if (disableButton != null && disableButton.gameObject.activeInHierarchy && disableButton.interactable)
            return disableButton.gameObject;
        return null;
    }

    private void RebuildNavScope()
    {
        var scope = GetComponentInParent<UINavScope>();
        if (scope != null && scope.isActiveAndEnabled) scope.Rebuild();
    }
}