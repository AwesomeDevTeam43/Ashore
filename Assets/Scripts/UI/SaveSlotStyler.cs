using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applies consistent styling to Save Slot UI elements to match the game's ocean theme.
/// Attach this to the SaveSlotManager or any parent object, and assign the slot buttons.
/// </summary>
public class SaveSlotStyler : MonoBehaviour
{
    [Header("Save Slot Buttons")]
    [SerializeField] private Button[] slotButtons;
    
    [Header("Style Settings")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 0.85f, 1f);
    [SerializeField] private Color highlightedColor = new Color(0.3f, 0.75f, 0.95f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.15f, 0.5f, 0.7f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.75f, 0.95f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    [Header("Text Settings")]
    [SerializeField] private float titleFontSize = 36f;
    [SerializeField] private float infoFontSize = 24f;
    [SerializeField] private Color textColor = Color.white;
    
    [Header("Size Settings")]
    [SerializeField] private Vector2 buttonSize = new Vector2(400f, 80f);
    [SerializeField] private float buttonSpacing = 20f;

    void Start()
    {
        ApplyStyleToAllSlots();
    }

    [ContextMenu("Apply Style to All Slots")]
    public void ApplyStyleToAllSlots()
    {
        if (slotButtons == null) return;
        
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
            {
                ApplyButtonStyle(slotButtons[i], i + 1);
            }
        }
    }

    private void ApplyButtonStyle(Button button, int slotNumber)
    {
        // Apply button colors
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = selectedColor;
        colors.disabledColor = disabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.15f;
        button.colors = colors;
        
        // Apply button size
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = buttonSize;
        }
        
        // Style the button's Image (remove the white tint)
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = Color.white; // Color is controlled by Button component
        }
        
        // Find and style children text elements
        TextMeshProUGUI[] texts = button.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var tmp in texts)
        {
            tmp.color = textColor;
            tmp.fontStyle = FontStyles.Bold;
            
            // First text is usually the title, second is info
            if (tmp.transform == button.transform.GetChild(0))
            {
                tmp.fontSize = titleFontSize;
                tmp.text = $"Slot {slotNumber}";
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }
        
        // Add hover effect if not present
        if (button.GetComponent<ButtonHoverEffect>() == null)
        {
            var hoverEffect = button.gameObject.AddComponent<ButtonHoverEffect>();
        }
        
        // Add navigation components for gamepad support
        if (button.GetComponent<SelectionHighlight>() == null)
        {
            button.gameObject.AddComponent<SelectionHighlight>();
        }
        
        // Set automatic navigation
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-find slot buttons if not assigned
        if (slotButtons == null || slotButtons.Length == 0)
        {
            var saveSlotMenu = GetComponent<SaveSlotMenu>();
            if (saveSlotMenu != null && saveSlotMenu.slots != null)
            {
                slotButtons = new Button[saveSlotMenu.slots.Length];
                for (int i = 0; i < saveSlotMenu.slots.Length; i++)
                {
                    slotButtons[i] = saveSlotMenu.slots[i].slotButton;
                }
            }
        }
    }
#endif
}
