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

        // Wire slot buttons to show details automatically: make the slot root itself a clickable Button.
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i; // capture

            // Make sure any child icon doesn't block raycasts
            var childImages = slots[i].GetComponentsInChildren<Image>(true);
            foreach (var im in childImages)
            {
                if (im.gameObject != slots[i].gameObject && im.name == "Icon")
                {
                    im.raycastTarget = false;
                }
            }

            // Ensure the slot root has an Image to act as button targetGraphic
            Image rootImg = slots[i].GetComponent<Image>();
            if (rootImg == null)
            {
                rootImg = slots[i].gameObject.AddComponent<Image>();
                rootImg.color = new Color(1f, 1f, 1f, 0f);
                rootImg.raycastTarget = true;
            }

            // Ensure the slot root has a Button
            Button slotBtn = slots[i].GetComponent<Button>();
            if (slotBtn == null)
            {
                slotBtn = slots[i].gameObject.AddComponent<Button>();
                slotBtn.targetGraphic = rootImg;
                slotBtn.interactable = true;
            }

            // Wire click
            slotBtn.onClick.RemoveAllListeners();
            slotBtn.onClick.AddListener(() => { Debug.Log($"Inventoy_UI: slot button clicked index={index} slot='{slots[index].name}'"); ShowDetails(index); });

            // Also wire the slot's select callback
            slots[i].onSelected = (s) => { ShowDetails(index); };

            // Add a low-level SlotClickCatcher to forward pointer clicks directly to the UI (fallback for edge cases)
            var catcher = slots[i].GetComponent<SlotClickCatcher>();
            if (catcher == null)
            {
                catcher = slots[i].gameObject.AddComponent<SlotClickCatcher>();
            }
            catcher.ui = this;
            catcher.slotIndex = index;

            // Diagnostics for blocking UI: check for CanvasGroup on any parent that might block interactions
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

            // If the item is craftable, append the required materials and current player counts
            if (it.isCraftable && it.craftIngredients != null && it.craftIngredients.Count > 0)
            {
                description += "\n\nRequired:\n";
                foreach (var ing in it.craftIngredients)
                {
                    if (ing == null || ing.material == null) continue;
                    int have = Inventory.instance != null ? Inventory.instance.GetItemQuantity(ing.material) : 0;
                    description += $" - {ing.material.itemName} x{ing.amount} (You have: {have})\n";
                }
            }

            descriptionText.text = description;
        }

        if (descriptionButton != null)
        {
            if (descriptionSecondaryButton != null) descriptionSecondaryButton.gameObject.SetActive(false);

            // Clear previous listeners always
            descriptionButton.onClick.RemoveAllListeners();

            // Prefer Craft when isCraftable (even for equipment types)
            if (it.isCraftable)
            {
                descriptionButton.gameObject.SetActive(true);
                descriptionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Craft";

                // Determine if player has required materials
                bool canCraft = false;
                if (Inventory.instance != null)
                {
                    canCraft = it.CanCraft((ItemData m) => Inventory.instance.GetItemQuantity(m));
                }

                descriptionButton.interactable = canCraft;
                if (canCraft)
                {
                    descriptionButton.onClick.AddListener(() => TryCraftSelectedItem());
                }

                if (descriptionSecondaryButton != null)
                {
                    descriptionSecondaryButton.gameObject.SetActive(true);
                    descriptionSecondaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "Craft";
                    descriptionSecondaryButton.onClick.RemoveAllListeners();
                    if (canCraft) descriptionSecondaryButton.onClick.AddListener(() => TryCraftSelectedItem());
                }
            }
            else
            {
                // Not craftable: check for equipment first, then usable
                EquipmentData ed = it as EquipmentData;
                if (ed != null)
                {
                    descriptionButton.gameObject.SetActive(true);
                    descriptionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Equip";
                    descriptionButton.interactable = true;
                    int capturedIndex = selectedSlotIndex;
                    descriptionButton.onClick.AddListener(() =>
                    {
                        if (capturedIndex >= 0 && capturedIndex < slots.Length)
                        {
                            slots[capturedIndex].OnUseItem();
                        }
                    });
                }
                else if (it.isUsable)
                {
                    descriptionButton.gameObject.SetActive(true);
                    descriptionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Use";
                    descriptionButton.interactable = true;
                    // If you have a use handler, wire it here. For now keep it enabled but without action.
                }
                else
                {
                    descriptionButton.gameObject.SetActive(false);
                }
            }
        }

    }

    // Public entry used by SlotClickCatcher to forward pointer clicks reliably.
    public void OnSlotClicked(int slotIndex)
    {
        Debug.Log($"Inventoy_UI: OnSlotClicked({slotIndex}) called");
        ShowDetails(slotIndex);
    }

    // Diagnostic helper: given a screen position, log which slot RectTransforms contain that point
    public void LogSlotHitInfo(Vector2 screenPos)
    {
        if (slots == null || slots.Length == 0)
        {
            Debug.Log("Inventoy_UI: no slots to test");
            return;
        }

        Debug.Log($"Inventoy_UI DIAG: checking slots for screenPos={screenPos}");
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            RectTransform rt = s.GetComponent<RectTransform>();
            bool contains = false;
            Camera cam = null;
            var c = s.GetComponentInParent<Canvas>();
            if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) cam = c.worldCamera;
            if (rt != null)
            {
                contains = RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
            }

            Image rootImg = s.GetComponent<Image>();
            bool rootRaycast = rootImg != null ? rootImg.raycastTarget : false;
            CanvasGroup cg = s.GetComponentInParent<CanvasGroup>();
            Mask mask = s.GetComponentInParent<Mask>();

            Debug.Log($"  Slot[{i}]='{s.name}' contains={contains} rootImgRaycast={rootRaycast} Canvas={(c!=null?c.name:"none")} CanvasGroup={(cg!=null?cg.name+" alpha="+cg.alpha+" interactable="+cg.interactable:"none")} Mask={(mask!=null?mask.name:"none")}");
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

    // Attempt to craft the currently selected item (uses ItemData.craftIngredients and Inventory methods)
    private void TryCraftSelectedItem()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Length) return;

        ItemData it = slots[selectedSlotIndex].GetItem();
        if (it == null) return;

        if (!it.isCraftable)
        {
            Debug.Log("Inventoy_UI: item is not craftable.");
            return;
        }

        // Check inventory for required materials using Inventory.GetItemQuantity
        if (it.craftIngredients != null)
        {
            foreach (var ing in it.craftIngredients)
            {
                if (ing == null || ing.material == null)
                {
                    Debug.LogWarning("Inventoy_UI: invalid craft ingredient.");
                    return;
                }

                int have = Inventory.instance != null ? Inventory.instance.GetItemQuantity(ing.material) : 0;
                if (have < ing.amount)
                {
                    Debug.Log("Inventoy_UI: Cannot craft - missing materials.");
                    return;
                }
            }
        }

        // All checks passed. Now perform the transaction: remove materials and remove one recipe item (the craftable entry)
        // We'll rollback everything if adding the crafted product fails.

        // Remove materials
        foreach (var ing in it.craftIngredients)
        {
            Inventory.instance.Remove(ing.material, ing.amount);
        }

        // Remove one unit of the recipe item from inventory (the recipe is consumed)
        Inventory.instance.Remove(it, 1);

        // Determine resulting ItemData to add. Prefer a configured craftResult ScriptableObject.
        ItemData result = null;
        bool createdRuntimeClone = false;

        ItemData proto = it.craftResult != null ? it.craftResult : it; // source prototype to clone

        // Create a runtime instance of the same concrete ScriptableObject type as the prototype
        var runtimeInstance = ScriptableObject.CreateInstance(proto.GetType()) as ItemData;
        if (runtimeInstance == null)
        {
            Debug.LogWarning("Inventoy_UI: failed to create runtime instance for crafted item. Aborting craft.");
            // rollback materials & recipe
            Inventory.instance.Add(it, 1);
            foreach (var ing in it.craftIngredients)
            {
                Inventory.instance.Add(ing.material, ing.amount);
            }
            return;
        }

        // Copy base ItemData fields
        runtimeInstance.itemName = proto.itemName;
        runtimeInstance.icon = proto.icon;
        runtimeInstance.description = proto.description;
        runtimeInstance.isUsable = proto.isUsable;
        runtimeInstance.isCraftable = false; // crafted product shouldn't be a recipe
        runtimeInstance.isStackable = proto.isStackable;
        runtimeInstance.maxStackSize = proto.maxStackSize;

        // If the prototype is an EquipmentData, copy equipment-specific fields as well
        var protoEquip = proto as EquipmentData;
        var runtimeEquip = runtimeInstance as EquipmentData;
        if (protoEquip != null && runtimeEquip != null)
        {
            runtimeEquip.equipmentPrefab = protoEquip.equipmentPrefab;
        }

        result = runtimeInstance;
        createdRuntimeClone = true;

        bool added = Inventory.instance.Add(result, 1);
        if (added)
        {
            Debug.Log($"Inventoy_UI: Crafted {result.itemName} and added to inventory.");
            UpdateUI();
            ClearSelection();
        }
        else
        {
            // Add failed: rollback recipe and materials
            Debug.LogWarning("Inventoy_UI: Crafted item could not be added to inventory (maybe full). Rolling back recipe and materials.");
            // restore recipe
            Inventory.instance.Add(it, 1);
            // restore materials
            foreach (var ing in it.craftIngredients)
            {
                Inventory.instance.Add(ing.material, ing.amount);
            }

            // destroy runtime clone if we created one since it's unused
            if (createdRuntimeClone && result != null)
            {
                Destroy(result);
            }
        }
    }
}