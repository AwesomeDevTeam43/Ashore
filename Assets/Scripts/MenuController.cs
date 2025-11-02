using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [Header("Root & Pause")]
    [SerializeField] private GameObject menuRoot; // Assign MenuCanvas root
    [SerializeField] private bool pauseOnOpen = true;

    [Header("Tabs")] 
    [SerializeField] private Button inventoryTab;
    [SerializeField] private Button equipmentTab;
    [SerializeField] private Button mapTab;
    // Gadgets tab removed per request

    [Header("Pages")] 
    [SerializeField] private GameObject inventoryPageGO;
    [SerializeField] private GameObject equipmentPageGO;
    [SerializeField] private GameObject mapPageGO;
    // Gadgets page removed per request

    [Header("Page Behaviours (optional)")]
    [SerializeField] private InventoryPage inventoryPage; // assign if InventoryPage is on inventoryPageGO
    [SerializeField] private EquipmentPage equipmentPage; // optional, for simple equipped display
    [SerializeField] private GameObject defaultEquipmentFocus;
    [SerializeField] private GameObject defaultMapFocus;
    [Header("Footer")]
    [SerializeField] private TextMeshProUGUI footerText; // Displays selected item info

    [Header("UI Actions (for closing while Player map is disabled)")]
    [Tooltip("UI action that should toggle/close the inventory while the menu is open (e.g., UI/Inventory).")]
    [SerializeField] private InputActionReference uiInventoryAction;
    [Tooltip("Optional UI Cancel action (e.g., B/Circle or Esc) to close the menu.")]
    [SerializeField] private InputActionReference uiCancelAction;
    [SerializeField] private bool closeOnUICancel = true;
    [Tooltip("UI action for moving to the previous tab (e.g., LB).")]
    [SerializeField] private InputActionReference uiLeftTabAction;
    [Tooltip("UI action for moving to the next tab (e.g., RB).")]
    [SerializeField] private InputActionReference uiRightTabAction;

    private int currentTabIndex = 0; // 0=Inventory,1=Equipment,2=Map
    private EventSystem es;
    private Player_InputHandler inputHandler;
    private PlayerInput playerInput;
    private bool prevInventoryTriggered = false;
    private InputAction cachedUIInventory;
    private InputAction cachedUICancel;
    private InputAction cachedUILeftTab;
    private InputAction cachedUIRightTab;

    private void Awake()
    {
        es = EventSystem.current;
        if (menuRoot == null)
        {
            menuRoot = this.gameObject; // assume this script is on the MenuCanvas root
        }

        // Subscribe to inventory input as early as possible
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            inputHandler = playerObj.GetComponent<Player_InputHandler>();
            playerInput = playerObj.GetComponent<PlayerInput>();
        }
        if (inputHandler == null)
        {
            inputHandler = FindFirstObjectByType<Player_InputHandler>();
        }
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }
        if (inputHandler != null)
        {
            inputHandler.OnInventoryPressed += ToggleMenu;
        }

        // Start with menu hidden
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
    }

    private void Start()
    {
        // Wire tab buttons
    if (inventoryTab != null) inventoryTab.onClick.AddListener(() => ShowPage(0));
    if (equipmentTab != null) equipmentTab.onClick.AddListener(() => ShowPage(1));
    if (mapTab != null) mapTab.onClick.AddListener(() => ShowPage(2));
        // Add highlight components for player feedback
    EnsureSelectionHighlight(inventoryTab != null ? inventoryTab.gameObject : null);
    EnsureSelectionHighlight(equipmentTab != null ? equipmentTab.gameObject : null);
    EnsureSelectionHighlight(mapTab != null ? mapTab.gameObject : null);
        EnsureSelectionHighlight(defaultEquipmentFocus);
    EnsureSelectionHighlight(defaultMapFocus);
    // no gadgets tab
    }

    private void Update()
    {
        // Detect current input mode (mouse vs controller/keyboard) each frame while menu is open
        if (menuRoot != null && menuRoot.activeInHierarchy)
        {
            UIInputMode.DetectThisFrame();
        }
        // Edge-detect as fallback in case event missed (should rarely be needed)
        if (inputHandler != null)
        {
            bool now = inputHandler.InventoryActionTriggered;
            if (now && !prevInventoryTriggered)
            {
                ToggleMenu();
            }
            prevInventoryTriggered = now;
        }

        // Keyboard fallback for tab switching (optional): Q/E
        if (menuRoot != null && menuRoot.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Q)) PrevTab();
            if (Input.GetKeyDown(KeyCode.E)) NextTab();

            // Update footer with currently selected inventory item (if any)
            if (footerText != null && es != null)
            {
                var sel = es.currentSelectedGameObject;
                if (sel != null)
                {
                    var slot = sel.GetComponent<InventorySlot>();
                    if (slot != null)
                    {
                        var item = slot.GetItem();
                        int qty = slot.GetQuantity();
                        if (item != null)
                        {
                            string qtyStr = qty > 1 ? " x" + qty.ToString() : "";
                            footerText.text = $"{item.itemName}{qtyStr} — {item.description}";
                        }
                        else
                        {
                            footerText.text = string.Empty;
                        }
                    }
                    else
                    {
                        footerText.text = string.Empty;
                    }
                }
                else
                {
                    footerText.text = string.Empty;
                }
            }
        }
    }

    private void EnsureSelectionHighlight(GameObject go)
    {
        if (go == null) return;
    // Respect NoHighlight marker on this object (self only)
    if (go.GetComponent<NoHighlight>() != null) return;
        try
        {
            // Ensure there is a Graphic to render outline; add transparent Image if none
            var graphic = go.GetComponent<Graphic>();
            if (graphic == null)
            {
                var img = go.GetComponent<Image>();
                if (img == null)
                {
                    img = go.AddComponent<Image>();
                }
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = true;
                graphic = img;
            }

            if (go.GetComponent<SelectionHighlight>() == null)
            {
                go.AddComponent<SelectionHighlight>();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"MenuController: Failed to attach highlight to '{go?.name}': {ex.Message}");
        }
    }

    private void EnsureHighlightsForAllInteractables()
    {
        if (menuRoot == null) return;
        // Add highlight to all Selectables and their targetGraphics
        var selectables = menuRoot.GetComponentsInChildren<Selectable>(true);
        foreach (var s in selectables)
        {
            if (s == null) continue;
            if (s.GetComponent<NoHighlight>() == null && s.interactable && s.IsActive())
            {
                EnsureSelectionHighlight(s.gameObject);
            }
            if (s.targetGraphic != null)
            {
                var tgGo = s.targetGraphic.gameObject;
                if (tgGo != s.gameObject && tgGo.GetComponent<NoHighlight>() == null)
                {
                    EnsureSelectionHighlight(tgGo);
                }
            }
        }
        // Add highlight to any Graphic that has raycastTarget enabled (e.g., TMP text buttons)
        var graphics = menuRoot.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            if (g != null && g.raycastTarget && g.gameObject.GetComponent<NoHighlight>() == null)
            {
                EnsureSelectionHighlight(g.gameObject);
            }
        }
    }

    public void ToggleMenu()
    {
        if (menuRoot == null) return;
        bool newState = !menuRoot.activeSelf;
        if (newState) OpenMenu(); else CloseMenu();
    }

    public void OpenMenu()
    {
        if (menuRoot == null) return;
        menuRoot.SetActive(true);
        if (pauseOnOpen) Time.timeScale = 0f;
        // Disable gameplay inputs while menu is open
        if (inputHandler != null)
        {
            inputHandler.DisablePlayerActions();
        }
        // Switch PlayerInput to UI map if available (ensures UI actions are paired to the same user)
        if (playerInput != null)
        {
            try { playerInput.SwitchCurrentActionMap("UI"); } catch { }
        }
        EnableUIShortcuts();
        EnsureHighlightsForAllInteractables();
        ShowPage(currentTabIndex);
    }

    public void CloseMenu()
    {
        if (menuRoot == null) return;
        if (pauseOnOpen) Time.timeScale = 1f;
        // Clear selection (optional)
        if (es != null) es.SetSelectedGameObject(null);
        menuRoot.SetActive(false);
        DisableUIShortcuts();
        // Re-enable gameplay inputs when closing menu
        if (inputHandler != null)
        {
            inputHandler.EnablePlayerActions();
        }
        // Switch back to Player map after closing
        if (playerInput != null)
        {
            try { playerInput.SwitchCurrentActionMap("Player"); } catch { }
        }
    }

    private void OnDestroy()
    {
        DisableUIShortcuts();
        if (inputHandler != null)
        {
            inputHandler.OnInventoryPressed -= ToggleMenu;
        }
    }

    private void EnableUIShortcuts()
    {
        // Resolve actions from PlayerInput instance first (paired to the same InputUser),
        // fall back to serialized InputActionReferences.
        cachedUIInventory = ResolveUIAction("UI/Inventory", uiInventoryAction);
        cachedUICancel = closeOnUICancel ? ResolveUIAction("UI/Cancel", uiCancelAction) : null;
        cachedUILeftTab = ResolveUIAction("UI/LeftTab", uiLeftTabAction);
        cachedUIRightTab = ResolveUIAction("UI/RightTab", uiRightTabAction);

        if (cachedUIInventory != null)
        {
            cachedUIInventory.performed -= OnUIInventoryPerformed;
            cachedUIInventory.performed += OnUIInventoryPerformed;
            if (!cachedUIInventory.enabled) cachedUIInventory.Enable();
        }
        if (cachedUICancel != null)
        {
            cachedUICancel.performed -= OnUICancelPerformed;
            cachedUICancel.performed += OnUICancelPerformed;
            if (!cachedUICancel.enabled) cachedUICancel.Enable();
        }
        if (cachedUILeftTab != null)
        {
            cachedUILeftTab.performed -= OnUILeftTabPerformed;
            cachedUILeftTab.performed += OnUILeftTabPerformed;
            if (!cachedUILeftTab.enabled) cachedUILeftTab.Enable();
        }
        if (cachedUIRightTab != null)
        {
            cachedUIRightTab.performed -= OnUIRightTabPerformed;
            cachedUIRightTab.performed += OnUIRightTabPerformed;
            if (!cachedUIRightTab.enabled) cachedUIRightTab.Enable();
        }
    }

    private void DisableUIShortcuts()
    {
        if (cachedUIInventory != null)
        {
            cachedUIInventory.performed -= OnUIInventoryPerformed;
            if (cachedUIInventory.enabled) cachedUIInventory.Disable();
            cachedUIInventory = null;
        }
        if (cachedUICancel != null)
        {
            cachedUICancel.performed -= OnUICancelPerformed;
            if (cachedUICancel.enabled) cachedUICancel.Disable();
            cachedUICancel = null;
        }
        if (cachedUILeftTab != null)
        {
            cachedUILeftTab.performed -= OnUILeftTabPerformed;
            if (cachedUILeftTab.enabled) cachedUILeftTab.Disable();
            cachedUILeftTab = null;
        }
        if (cachedUIRightTab != null)
        {
            cachedUIRightTab.performed -= OnUIRightTabPerformed;
            if (cachedUIRightTab.enabled) cachedUIRightTab.Disable();
            cachedUIRightTab = null;
        }
    }

    private InputAction ResolveUIAction(string path, InputActionReference fallback)
    {
        // Try PlayerInput instance first
        if (playerInput != null && playerInput.actions != null)
        {
            var act = playerInput.actions.FindAction(path, false);
            if (act != null) return act;
        }
        // Fallback to serialized reference
        return fallback != null ? fallback.action : null;
    }

    private void OnUIInventoryPerformed(InputAction.CallbackContext ctx)
    {
        // Toggle the menu from UI action (works even when Player map is disabled)
        ToggleMenu();
    }

    private void OnUICancelPerformed(InputAction.CallbackContext ctx)
    {
        if (menuRoot != null && menuRoot.activeSelf)
        {
            CloseMenu();
        }
    }

    private void OnUILeftTabPerformed(InputAction.CallbackContext ctx)
    {
        if (menuRoot != null && menuRoot.activeSelf)
        {
            PrevTab();
        }
    }

    private void OnUIRightTabPerformed(InputAction.CallbackContext ctx)
    {
        if (menuRoot != null && menuRoot.activeSelf)
        {
            NextTab();
        }
    }


    public void NextTab()
    {
        int tabs = GetTabCount();
        int next = (currentTabIndex + 1 + tabs) % tabs;
        ShowPage(next);
    }

    public void PrevTab()
    {
        int tabs = GetTabCount();
        int prev = (currentTabIndex - 1 + tabs) % tabs;
        ShowPage(prev);
    }

    private int GetTabCount()
    {
        int tabs = 0;
        if (inventoryPageGO != null) tabs++;
        if (equipmentPageGO != null) tabs++;
        if (mapPageGO != null) tabs++;
        return Mathf.Max(1, tabs);
    }

    public void ShowPage(int index)
    {
        int tabs = GetTabCount();
        currentTabIndex = Mathf.Clamp(index, 0, tabs - 1);

        if (inventoryPageGO != null) inventoryPageGO.SetActive(currentTabIndex == 0);
        if (equipmentPageGO != null) equipmentPageGO.SetActive(currentTabIndex == 1);
        if (mapPageGO != null) mapPageGO.SetActive(currentTabIndex == 2);

        // Focus
        if (es == null) es = EventSystem.current;
        GameObject focus = null;
        switch (currentTabIndex)
        {
            case 0:
                if (inventoryPage != null) inventoryPage.Refresh();
                focus = inventoryPage != null ? inventoryPage.GetFirstSelectable() : (inventoryTab != null ? inventoryTab.gameObject : null);
                break;
            case 1:
                if (equipmentPage != null) equipmentPage.Refresh();
                focus = defaultEquipmentFocus != null ? defaultEquipmentFocus : (equipmentTab != null ? equipmentTab.gameObject : null);
                break;
            case 2:
                focus = defaultMapFocus != null ? defaultMapFocus : (mapTab != null ? mapTab.gameObject : null);
                break;
        }
        if (es != null && focus != null)
        {
            es.SetSelectedGameObject(focus);
        }
    }
}
