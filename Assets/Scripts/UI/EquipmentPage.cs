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

    private void OnValidate()
    {
        // Best-effort auto-wire to reduce Inspector setup errors.
        AutoWireMissingRefs();
    }

    private void AutoWireMissingRefs()
    {
        // Prefer explicit child names to avoid grabbing background images.
        if (icon == null)
        {
            var t = FindChildByName("Icon");
            if (t != null) icon = t.GetComponent<Image>();
        }

        if (mainWeaponIconUI == null)
        {
            var t = FindChildByName("MainWeaponIcon") ?? FindChildByName("WeaponIcon");
            if (t != null) mainWeaponIconUI = t.GetComponent<Image>();
        }

        if (nameText == null)
        {
            var t = FindChildByName("Name") ?? FindChildByName("ItemName");
            if (t != null) nameText = t.GetComponent<TextMeshProUGUI>();
        }

        if (descriptionText == null)
        {
            var t = FindChildByName("Description") ?? FindChildByName("ItemDescription");
            if (t != null) descriptionText = t.GetComponent<TextMeshProUGUI>();
        }
    }

    private Transform FindChildByName(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName)) return null;
        var all = GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (t == null) continue;
            if (string.Equals(t.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return t;
        }
        return null;
    }

    private void Awake()
    {
        AutoWireMissingRefs();

        if (disableButton != null)
        {
            disableButton.onClick.RemoveAllListeners();
            disableButton.onClick.AddListener(OnDisablePressed);
            // Ensure visual highlight on hover/selection
            var hi = disableButton.GetComponent<SelectionHighlight>();
            if (hi == null) disableButton.gameObject.AddComponent<SelectionHighlight>();
        }

        if (setMeleeButton != null)
        {
            setMeleeButton.onClick.RemoveAllListeners();
            setMeleeButton.onClick.AddListener(() => SetMainWeapon(Player_Controller.MainWeaponType.Melee));
            var hi = setMeleeButton.GetComponent<SelectionHighlight>();
            if (hi == null) setMeleeButton.gameObject.AddComponent<SelectionHighlight>();
        }
        if (setRangedButton != null)
        {
            setRangedButton.onClick.RemoveAllListeners();
            setRangedButton.onClick.AddListener(() => SetMainWeapon(Player_Controller.MainWeaponType.Ranged));
            var hi = setRangedButton.GetComponent<SelectionHighlight>();
            if (hi == null) setRangedButton.gameObject.AddComponent<SelectionHighlight>();
        }

        if (toggleMainWeaponButton != null)
        {
            toggleMainWeaponButton.onClick.RemoveAllListeners();
            toggleMainWeaponButton.onClick.AddListener(ToggleMainWeapon);
            var hi = toggleMainWeaponButton.GetComponent<SelectionHighlight>();
            if (hi == null) toggleMainWeaponButton.gameObject.AddComponent<SelectionHighlight>();
        }
    }

    private void OnEnable()
    {
        AutoWireMissingRefs();

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
        AutoWireMissingRefs();

        if (player == null)
        {
            SetNone();
            return;
        }

        var eq = player.CurrentEquipment;
        if (eq != null && eq.equipmentData != null)
        {
            if (icon != null)
            {
                icon.sprite = eq.equipmentData.icon;
                icon.enabled = icon.sprite != null;
                // If alpha was accidentally set to 0 in prefab, make sure it is visible when sprite exists.
                if (icon.enabled && icon.color.a < 0.01f)
                {
                    var c = icon.color;
                    c.a = 1f;
                    icon.color = c;
                }
            }
            if (nameText != null) nameText.text = eq.equipmentData.itemName;
            if (descriptionText != null) descriptionText.text = eq.equipmentData.description;
            if (disableButton != null)
            {
                disableButton.gameObject.SetActive(true);
                var label = disableButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "Unequip";
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
            var mwSprite = player.CurrentMainWeapon == Player_Controller.MainWeaponType.Melee ? meleeIcon : rangedIcon;
            mainWeaponIconUI.sprite = mwSprite;
            mainWeaponIconUI.enabled = mwSprite != null;
            if (mainWeaponIconUI.enabled && mainWeaponIconUI.color.a < 0.01f)
            {
                var c = mainWeaponIconUI.color;
                c.a = 1f;
                mainWeaponIconUI.color = c;
            }
        }

        RebuildNavScope();
    }

    private void SetNone()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
        if (nameText != null) nameText.text = "None";
        if (descriptionText != null) descriptionText.text = "";
        if (disableButton != null)
        {
            var label = disableButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = "Unequip";
            disableButton.interactable = false;
        }

        if (mainWeaponIconUI != null)
        {
            // Still show main weapon icon if configured; if not, hide it.
            var mwSprite = player != null
                ? (player.CurrentMainWeapon == Player_Controller.MainWeaponType.Melee ? meleeIcon : rangedIcon)
                : null;
            mainWeaponIconUI.sprite = mwSprite;
            mainWeaponIconUI.enabled = mwSprite != null;
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
        // Set up navigation between available buttons
        SetupButtonNavigation();
    }

    private void SetupButtonNavigation()
    {
        // Use Automatic navigation - Unity handles it well for simple button lists
        Button[] buttons = { toggleMainWeaponButton, setMeleeButton, setRangedButton, disableButton };
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Automatic;
            btn.navigation = nav;
        }
    }
}