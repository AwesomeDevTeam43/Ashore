using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// Note: this script automatically finds InventorySlot components under `itemsParent` and
// wires their Button (if present) to show the description UI. Assign the Description
// panel's child Image/Text/Button in the Inspector to enable automatic details and use.

public class Inventoy_UI : MonoBehaviour
{
    // --- Your existing fields ---
    private bool isOpen;
    [SerializeField] private GameObject canvasInventory;
    [SerializeField] private GameObject playerUI;

    // --- Fields for updating the slots ---
    [SerializeField] private Transform itemsParent; // Drag the parent of your slots here
    private Inventory inventory;
    private InventorySlot[] slots;

    [Header("Description UI (assign) ")]
    [SerializeField] private Image descriptionImage;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button descriptionButton;
    [SerializeField] private Button descriptionSecondaryButton; // optional secondary action (e.g., Craft)

    [Header("Behavior")]
    [Tooltip("If true, opening the inventory will pause the game by setting Time.timeScale = 0. Set false to keep the game running while inventory is open.")]
    [SerializeField] private bool pauseOnOpen = false;

    // (Debug overlays and selection highlights removed)

    // internal selection index
    private int selectedSlotIndex = -1;

    void Start()
    {
        // Get the singleton instance of the inventory
        inventory = Inventory.instance;
        // Subscribe our UpdateUI method to the onItemChanged event
        inventory.onItemChangedCallback += UpdateUI;

        // Get all the slot components from the children of itemsParent
        if (itemsParent == null)
        {
            Debug.LogError("Inventoy_UI: itemsParent is not assigned in the inspector.");
            slots = new InventorySlot[0];
        }
        else
        {
            slots = itemsParent.GetComponentsInChildren<InventorySlot>();
        }

        Debug.Log($"Inventoy_UI: found {slots.Length} InventorySlot(s) under itemsParent '{itemsParent?.name}'");

        if (descriptionImage == null || descriptionText == null || descriptionButton == null)
        {
            Debug.LogWarning("Inventoy_UI: Description UI fields are not fully assigned (Image/Text/Button). Clicks will still populate, but the description panel may be incomplete.");
        }

        // EventSystem is required for UI Button clicks to work. Warn if missing.
        if (EventSystem.current == null)
        {
            Debug.LogWarning("Inventoy_UI: No EventSystem found in the scene. Creating one at runtime.");
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            // Note: EventSystem.current will be set automatically by Unity
        }

        // If canvasInventory is assigned, ensure it (or a parent) has a Canvas and GraphicRaycaster so UI responds to clicks
        if (canvasInventory != null)
        {
            Canvas c = canvasInventory.GetComponentInParent<Canvas>();
            if (c == null)
            {
                Debug.LogWarning($"Inventoy_UI: canvasInventory '{canvasInventory.name}' is not under a Canvas. UI elements may not render or receive clicks.");
            }
            else
            {
                GraphicRaycaster gr = c.GetComponent<GraphicRaycaster>();
                if (gr == null)
                {
                    Debug.LogWarning($"Inventoy_UI: Canvas '{c.name}' is missing GraphicRaycaster. Adding one so clicks are detected.");
                    c.gameObject.AddComponent<GraphicRaycaster>();
                }
            }
        }

        // (no debug overlays)

        // Wire slot buttons to show details automatically
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i; // capture
            // Prefer an existing Button on the slot. If none exists, try to find one on a child named "Button"
            Button slotBtn = slots[i].GetComponent<Button>();
            if (slotBtn == null)
            {
                // try a child Button (common prefab pattern)
                Transform btnChild = slots[i].transform.Find("Button");
                if (btnChild != null) slotBtn = btnChild.GetComponent<Button>();
            }

            if (slotBtn == null)
            {
                // Ensure the slot root has an Image to act as the targetGraphic for a Button.
                Image rootImg = slots[i].GetComponent<Image>();
                if (rootImg == null)
                {
                    // Add a transparent Image to the slot root so the Button has a targetGraphic
                    rootImg = slots[i].gameObject.AddComponent<Image>();
                    rootImg.color = new Color(1, 1, 1, 0);
                    rootImg.raycastTarget = true;
                }

                // Add a Button component to the slot root so clicks naturally hit the slot even if the icon overlays it
                slotBtn = slots[i].gameObject.AddComponent<Button>();
                slotBtn.targetGraphic = rootImg;
                slotBtn.interactable = true;

                Debug.LogWarning($"Inventoy_UI: Slot '{slots[i].name}' had no Button; added Button on root for reliable clicks.");
            }

            // Wire the Button click to show details and log the click for diagnostics
            slotBtn.onClick.AddListener(() => { Debug.Log($"Inventoy_UI: slot button clicked index={index} slot='{slots[index].name}'"); ShowDetails(index); });

            // Also wire the slot's select callback (useful if the icon intercepts clicks)
            slots[i].onSelected = (s) =>
            {
                ShowDetails(index);
            };

            // no debug overlay/selection highlight

            // Run quick diagnostics for blocking UI: check for CanvasGroup on any parent that might block interactions
            CanvasGroup cg = slots[i].GetComponentInParent<CanvasGroup>();
            if (cg != null && (cg.alpha == 0f || !cg.interactable))
            {
                Debug.LogWarning($"Inventoy_UI: slot '{slots[i].name}' is inside CanvasGroup '{cg.name}' with alpha={cg.alpha} interactable={cg.interactable} which may block clicks.");
            }
        }

        // Diagnostic: report slot wiring state so we can find non-clickable slots
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            bool hasButton = s.GetComponent<Button>() != null;
            Image rootImg = s.GetComponent<Image>();
            bool rootImgRaycast = rootImg != null ? rootImg.raycastTarget : false;
            Image childIcon = null;
            var imgs = s.GetComponentsInChildren<Image>(true);
            foreach (var im in imgs)
            {
                if (im.gameObject != s.gameObject) { childIcon = im; break; }
            }
            string iconName = childIcon != null && childIcon.sprite != null ? childIcon.sprite.name : "(none)";
            CanvasGroup cg = s.GetComponentInParent<CanvasGroup>();
            Debug.Log($"Inventoy_UI DIAG: Slot[{i}]='{s.name}' hasButton={hasButton} rootImg={(rootImg != null ? rootImg.name : "null")} rootImgRaycast={rootImgRaycast} childIcon={iconName} childIconRaycast={(childIcon != null ? childIcon.raycastTarget.ToString() : "n/a")} CanvasGroup={(cg != null ? cg.name + " alpha=" + cg.alpha + " interactable=" + cg.interactable : "none")} ");
        }

        // We'll wire description buttons dynamically in ShowDetails
        if (descriptionSecondaryButton != null)
        {
            descriptionSecondaryButton.gameObject.SetActive(false);
        }

        // Start with the inventory closed
        canvasInventory.SetActive(false);
        isOpen = false;

        // Ensure descriptionButton is disabled initially (no selection)
        if (descriptionButton != null)
        {
            descriptionButton.interactable = false;
        }

        // Populate UI initially in case inventory already has items
        UpdateUI();
    }

    void Update()
    {
        // Your existing toggle logic
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        isOpen = !isOpen;
        canvasInventory.SetActive(isOpen);

        // Also toggle the main player UI
        if (playerUI != null)
        {
            playerUI.SetActive(!isOpen);
        }

        // Optional: Pause the game when inventory is open (configurable)
        if (pauseOnOpen)
        {
            Time.timeScale = isOpen ? 0f : 1f;
        }

        // When opening the inventory, refresh UI so elements created while it was inactive are laid out and visible
        if (isOpen)
        {
            // Force a UI rebuild to ensure rects/layouts are updated
            UpdateUI();
            Canvas.ForceUpdateCanvases();
            RectTransform itemsRT = itemsParent as RectTransform;
            if (itemsRT != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(itemsRT);
            }
        }
    }

    // This method is called whenever an item is added or removed from the inventory
    void UpdateUI()
    {
        if (slots == null) return;

        // Debug: print inventory contents for diagnosis
        if (Inventory.instance != null)
        {
            string list = "Inventory contents: ";
            for (int i = 0; i < Inventory.instance.inventoryItems.Count; i++)
            {
                var it = Inventory.instance.inventoryItems[i];
                list += $"[{i}: {it?.itemData?.itemName} x{it?.quantity}] ";
            }
            Debug.Log(list);
        }

        // Loop through all of our slots
        for (int i = 0; i < slots.Length; i++)
        {
            // If there is an item for this slot
            if (i < inventory.inventoryItems.Count)
            {
                var inventoryItem = inventory.inventoryItems[i];
                slots[i].AddItem(inventoryItem.itemData, inventoryItem.quantity);
            }
            else
            {
                // Otherwise, clear the slot
                slots[i].ClearSlot();
            }
        }
    }

    // Show details for a given slot index
    private void ShowDetails(int slotIndex)
    {
        selectedSlotIndex = slotIndex;

        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            ClearSelection();
            return;
        }

        ItemData it = slots[slotIndex].GetItem();
        int qty = slots[slotIndex].GetQuantity();

        if (it == null)
        {
            if (descriptionImage != null) descriptionImage.sprite = null;
            if (descriptionText != null) descriptionText.text = "";
            if (descriptionButton != null)
            {
                descriptionButton.gameObject.SetActive(false);
            }
            return;
        }

        if (descriptionImage != null)
        {
            descriptionImage.sprite = it.icon;
        }

        if (descriptionText != null)
        {
            string description = it.description;

            // Add quantity to description
            if (qty > 1)
            {
                description = $"Quantity: {qty}\n\n{description}";
            }

            descriptionText.text = description;
        }

        if (descriptionButton != null)
        {
            if (descriptionSecondaryButton != null) descriptionSecondaryButton.gameObject.SetActive(false);

            EquipmentData ed = it as EquipmentData;
            if (ed != null)
            {
                descriptionButton.gameObject.SetActive(true);
                descriptionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Equip";
            }
            else
            {
                if (it.isUsable)
                {
                    descriptionButton.gameObject.SetActive(true);
                    descriptionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Use";
                }
                else if (it.isCraftable)
                {
                    descriptionButton.gameObject.SetActive(true);
                    descriptionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Craft";
                }
                else
                {
                    descriptionButton.gameObject.SetActive(false);
                }

                if (it.isCraftable && descriptionSecondaryButton != null)
                {
                    descriptionSecondaryButton.gameObject.SetActive(true);
                    descriptionSecondaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "Craft";
                }
                else if (descriptionSecondaryButton != null)
                {
                    descriptionSecondaryButton.gameObject.SetActive(false);
                }
            }
        }

    }

    private void ClearSelection()
    {
        selectedSlotIndex = -1;
        if (descriptionImage != null) descriptionImage.enabled = false;
        if (descriptionText != null) descriptionText.text = "";
        if (descriptionButton != null)
        {
            descriptionButton.onClick.RemoveAllListeners();
            descriptionButton.interactable = false;
        }
        if (descriptionSecondaryButton != null)
        {
            descriptionSecondaryButton.onClick.RemoveAllListeners();
            descriptionSecondaryButton.gameObject.SetActive(false);
        }

        // (selection highlight removed)
    }
}