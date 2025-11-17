using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryPage : MonoBehaviour
{
    [Header("Layout Generation")]
    [Tooltip("If true, this page will auto-create the grid and slot objects at runtime.")]
    [SerializeField] private bool autoBuildGrid = true;
    [Tooltip("Create at least this many slots if auto-build is enabled.")]
    [SerializeField] private int slotCount = 20;
    [Tooltip("If true, match or exceed Inventory.space when generating slots.")]
    [SerializeField] private bool matchInventoryCapacity = true;
    [Tooltip("How many columns in the grid.")]
    [SerializeField] private int columns = 5;
    [Tooltip("Cell size for each slot in the grid.")]
    [SerializeField] private Vector2 cellSize = new Vector2(96, 96);
    [Tooltip("Spacing between slots in the grid.")]
    [SerializeField] private Vector2 spacing = new Vector2(8, 8);
    [Tooltip("Padding for the grid inside its RectTransform bounds.")]
    [SerializeField] private RectOffset padding; // set default in OnValidate/Awake

    [Header("UI References (optional)")]
    [Tooltip("If not set and autoBuildGrid is on, a child named 'ItemsGrid' will be created.")]
    [SerializeField] private Transform itemsParent; // Parent that will contain InventorySlot objects
    [Tooltip("Context menu popup for item actions (Use/Equip/Drop).")]
    [SerializeField] private InventoryContextMenu contextMenu;
    [Header("Details Panel")]
    [SerializeField] private InventoryDetailsPanel detailsPanel;

    private Inventory inventory;
    private InventorySlot[] slots;
    private bool gridBuilt = false;

    private void OnValidate()
    {
        // Assign reasonable defaults in editor without calling setters in field initializers
        if (columns <= 0) columns = 5;
        if (cellSize.x <= 0 || cellSize.y <= 0) cellSize = new Vector2(96, 96);
        if (spacing.x < 0 || spacing.y < 0) spacing = new Vector2(8, 8);
        if (slotCount <= 0) slotCount = 20;
        if (padding == null) padding = new RectOffset(16, 16, 16, 16);
    }

    private void OnEnable()
    {
        EnsureInitialized();
        HideLegacyInvPanels();
        if (inventory != null)
        {
            inventory.onItemChangedCallback += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.onItemChangedCallback -= Refresh;
        }
    }

    // Some legacy prefabs include a background panel named "inv" behind slots.
    // Hide any such child to avoid a doubled background illusion.
    private void HideLegacyInvPanels()
    {
        // Look for direct or nested children named "inv" (case-insensitive)
        var tfs = GetComponentsInChildren<RectTransform>(true);
        foreach (var tf in tfs)
        {
            if (tf == null) continue;
            if (string.Equals(tf.gameObject.name, "inv", System.StringComparison.OrdinalIgnoreCase))
            {
                tf.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureInitialized()
    {
        if (inventory == null)
        {
            inventory = Inventory.instance;
            if (inventory == null)
            {
                Debug.LogWarning("InventoryPage: Inventory.instance not found in scene.");
            }
        }

        if (contextMenu == null)
        {
            contextMenu = GetComponentInChildren<InventoryContextMenu>(true);
            if (contextMenu == null)
            {
                // Create a lightweight menu as a child if missing
                var canvas = GetComponentInParent<Canvas>();
                Transform parent = canvas != null ? canvas.transform : this.transform;
                var go = new GameObject("InventoryContextMenu", typeof(RectTransform), typeof(InventoryContextMenu));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                contextMenu = go.GetComponent<InventoryContextMenu>();
            }
        }

        if (detailsPanel == null)
        {
            detailsPanel = GetComponentInChildren<InventoryDetailsPanel>(true);
            if (detailsPanel == null)
            {
                // Create a minimalist details panel anchored to the right side
                var go = new GameObject("InventoryDetailsPanel", typeof(RectTransform), typeof(CanvasRenderer));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(this.transform, false);
                rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(260, 0);
                rt.anchoredPosition = new Vector2(-8, 0);

                // Create text labels: Name, Description, CraftHeader, CraftCost
                InventoryDetailsPanel panel = go.AddComponent<InventoryDetailsPanel>();
                CreateTMP(rt, "Name", 22, FontStyles.Bold, new Vector2(0, -12));
                CreateTMP(rt, "Description", 18, FontStyles.Normal, new Vector2(0, -50));
                CreateTMP(rt, "CraftHeader", 20, FontStyles.Bold, new Vector2(0, -220));
                CreateTMP(rt, "CraftCost", 18, FontStyles.Normal, new Vector2(0, -250));
                detailsPanel = panel;
            }
        }

        if (autoBuildGrid && !gridBuilt)
        {
            BuildGridIfNeeded();
        }

        if (slots == null || slots.Length == 0)
        {
            if (itemsParent != null)
            {
                slots = itemsParent.GetComponentsInChildren<InventorySlot>(true);
                WireSlotButtons();
            }
        }
    }

    private void BuildGridIfNeeded()
    {
        if (itemsParent == null)
        {
            // Create a container under this GameObject
            var gridGO = new GameObject("ItemsGrid", typeof(RectTransform));
            var rt = gridGO.GetComponent<RectTransform>();
            rt.SetParent(this.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            itemsParent = gridGO.transform;
        }

        // Ensure GridLayoutGroup exists/configured
        var grid = itemsParent.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = itemsParent.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Compute rows from desired columns and slotCount
        int capacity = slotCount;
        if (matchInventoryCapacity && inventory != null)
        {
            capacity = Mathf.Max(capacity, inventory.space);
        }

        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.padding = padding;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);
        grid.childAlignment = TextAnchor.UpperLeft;

        // Clear existing children (optional) to avoid duplicates
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsParent.GetChild(i).gameObject);
        }

        // Create slot GameObjects
        for (int i = 0; i < capacity; i++)
        {
            var slotGO = new GameObject($"Slot_{i}", typeof(RectTransform));
            var srt = slotGO.GetComponent<RectTransform>();
            srt.SetParent(itemsParent, false);
            srt.localScale = Vector3.one;

            // Add InventorySlot (handles icon child itself)
            var slot = slotGO.AddComponent<InventorySlot>();

            // Add Image & Button for controller navigation
            var img = slotGO.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            var btn = slotGO.AddComponent<Button>();
            btn.targetGraphic = img;
        }

        // Cache slots
        slots = itemsParent.GetComponentsInChildren<InventorySlot>(true);
        gridBuilt = true;
        WireSlotButtons();
        RebuildNavScope();
    }

    private void WireSlotButtons()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            // Ensure the slot root has an Image to act as a targetGraphic/background
            Image rootImg = slot.GetComponent<Image>();
            if (rootImg == null)
            {
                rootImg = slot.gameObject.AddComponent<Image>();
                // Give slots a faint background so highlight is visible
                rootImg.color = new Color(0f, 0f, 0f, 0.18f);
                rootImg.raycastTarget = true;
            }
            else if (rootImg.color.a <= 0.01f)
            {
                // If background was fully transparent, give it a faint tint
                rootImg.color = new Color(0f, 0f, 0f, 0.18f);
            }

            // Ensure the slot root has a Button for controller navigation
            Button btn = slot.GetComponent<Button>();
            if (btn == null)
            {
                btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = rootImg;
            }

            // Mark as allowed navigation target (works with UINavScope whitelist)
            if (slot.GetComponent<UINavTarget>() == null)
            {
                slot.gameObject.AddComponent<UINavTarget>();
            }

            // Add/ensure selection highlight behavior
            var hi = slot.GetComponent<SelectionHighlight>();
            if (hi == null)
            {
                hi = slot.gameObject.AddComponent<SelectionHighlight>();
                // keep default colors; background tint handled internally when transparent
            }

            // Click behavior: left-click selects only; right-click opens context menu
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var esys = EventSystem.current;
                // Always treat as selection on left-click
                slot.Select();
            });

            // Wire right-click to open context menu
            var et = slot.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (et == null) et = slot.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            et.triggers ??= new System.Collections.Generic.List<UnityEngine.EventSystems.EventTrigger.Entry>();
            // Remove existing right-click entries to avoid duplicates
            et.triggers.RemoveAll(e => e.eventID == UnityEngine.EventSystems.EventTriggerType.PointerClick);
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
            entry.callback.AddListener((data) =>
            {
                var ped = data as UnityEngine.EventSystems.PointerEventData;
                if (ped != null && ped.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                {
                    var item = slot.GetItem();
                    if (item == null) return;
                    Vector2 screenPos = ped.position;
                    OpenContextMenu(slot, item, screenPos);
                }
            });
            et.triggers.Add(entry);

            // When a slot is selected (mouse or controller), update details panel
            slot.onSelected = (s) =>
            {
                var itm = s.GetItem();
                if (detailsPanel != null)
                {
                    detailsPanel.Show(itm, s.GetQuantity());
                }
                // Set EventSystem selection for controller navigation
                var es = EventSystem.current;
                if (es != null) es.SetSelectedGameObject(s.gameObject);
            };
        }
        RebuildNavScope();
    }

    public void Refresh()
    {
        EnsureInitialized();
        if (slots == null) return;
        if (inventory == null)
        {
            // Clear all slots if no inventory
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].ClearSlot();
            }
            if (detailsPanel != null) detailsPanel.Clear();
            return;
        }

        // Populate from Inventory
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventory.inventoryItems.Count)
            {
                var invItem = inventory.inventoryItems[i];
                slots[i].AddItem(invItem.itemData, invItem.quantity);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }

        // If there is no current selection, hide details panel
        var es = EventSystem.current;
        if (detailsPanel != null && (es == null || es.currentSelectedGameObject == null || es.currentSelectedGameObject.GetComponent<InventorySlot>() == null))
        {
            detailsPanel.Clear();
        }

        RebuildNavScope();
    }

    private void RebuildNavScope()
    {
        // Ensure UINavScope (if present up the hierarchy) includes freshly built/generated slots
        var scope = GetComponentInParent<UINavScope>();
        if (scope != null && scope.isActiveAndEnabled)
        {
            scope.Rebuild();
        }
    }

    public GameObject GetFirstSelectable()
    {
        // Prefer the first slot that has a Button
        if (slots != null)
        {
            foreach (var s in slots)
            {
                if (s == null) continue;
                var btn = s.GetComponent<Button>();
                if (btn != null && btn.interactable && s.gameObject.activeInHierarchy)
                {
                    return s.gameObject;
                }
            }
        }
        return null;
    }

    private void Update()
    {
        // Keyboard/controller handling: move selects; Enter/A opens context menu.
        UIInputMode.DetectThisFrame();
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var gp = UnityEngine.InputSystem.Gamepad.current;

        // Global cursor visibility is managed by UIInputModeManager. No per-page toggling here.

        // Open context menu on Enter (keyboard) or South (gamepad)
            bool open = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                        || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            if (InventoryContextMenu.IsOpenBlocked()) open = false;

        if (open)
        {
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null)
            {
                var slot = es.currentSelectedGameObject.GetComponent<InventorySlot>();
                if (slot != null)
                {
                    var item = slot.GetItem();
                    if (item != null)
                    {
                        Vector2 screenPos;
                        if (UIInputMode.Current != UIInputMode.Mode.Pointer)
                        {
                            // Non-pointer navigation (keyboard/gamepad): anchor menu to the slot center
                            var rt = slot.GetComponent<RectTransform>();
                            Camera cam = null;
                            var c = slot.GetComponentInParent<Canvas>();
                            if (c != null) cam = c.worldCamera;
                            screenPos = RectTransformUtility.WorldToScreenPoint(cam, rt != null ? rt.position : slot.transform.position);
                        }
                        else
                        {
                            // Pointer (mouse) mode: use actual mouse position for context menu
                            screenPos = UnityEngine.InputSystem.Mouse.current != null ? (Vector2)UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
                            if (screenPos == Vector2.zero)
                            {
                                var rt = slot.GetComponent<RectTransform>();
                                Camera cam = null;
                                var c = slot.GetComponentInParent<Canvas>();
                                if (c != null) cam = c.worldCamera;
                                screenPos = RectTransformUtility.WorldToScreenPoint(cam, rt != null ? rt.position : slot.transform.position);
                            }
                        }
                        OpenContextMenu(slot, item, screenPos);
                    }
                }
            }
        }

        // Mouse left click outside any item -> unselect and hide details panel
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (!PointerHitsInventoryItem(mouse.position.ReadValue()))
            {
                ClearSelectionAndPanel();
            }
        }
    }

    private void OpenContextMenu(InventorySlot slot, ItemData item, Vector2 screenPos)
    {
        contextMenu?.ShowFor(slot, item, screenPos);
    }

    private bool PointerHitsInventoryItem(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = screenPos };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        es.RaycastAll(ped, results);
        foreach (var r in results)
        {
            if (r.gameObject != null && r.gameObject.GetComponentInParent<InventorySlot>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void ClearSelectionAndPanel()
    {
        var es = EventSystem.current;
        if (es != null)
        {
            es.SetSelectedGameObject(null);
        }
        if (detailsPanel != null)
        {
            detailsPanel.Clear();
        }
    }

    private static void CreateTMP(RectTransform parent, string name, int size, TMPro.FontStyles style, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(0, size + 8);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
    tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.margin = new Vector4(8, 4, 8, 4);
        tmp.text = string.Empty;
    }
}
