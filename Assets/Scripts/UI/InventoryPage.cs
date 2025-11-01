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

            // Add/ensure selection highlight behavior
            var hi = slot.GetComponent<SelectionHighlight>();
            if (hi == null)
            {
                hi = slot.gameObject.AddComponent<SelectionHighlight>();
                // keep default colors; background tint handled internally when transparent
            }

            // Click behavior: if already selected, use/equip the item; otherwise, select it
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var esys = EventSystem.current;
                if (esys != null && esys.currentSelectedGameObject == slot.gameObject)
                {
                    // Use/equip when clicking the currently selected slot
                    var item = slot.GetItem();
                    if (item != null)
                    {
                        slot.OnUseItem();
                    }
                }
                else
                {
                    // First click selects
                    slot.Select();
                }
            });
        }
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
}
