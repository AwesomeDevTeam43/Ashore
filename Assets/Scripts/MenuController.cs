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

    public bool IsMenuOpen => menuRoot != null && menuRoot.activeSelf;

    private void Awake()
    {
        es = EventSystem.current;
        if (menuRoot == null)
        {
            menuRoot = this.gameObject; // assume this script is on the MenuCanvas root
        }
        FindAndSubscribeInputHandler();
        // Always enable inventory toggle in both Player and UI maps
        EnableInventoryActionInAllMaps();
        // Start with menu hidden
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        FindAndSubscribeInputHandler();
        EnableInventoryActionInAllMaps();
        DebugInventoryActionState("OnEnable");
    }

    private void OnDisable()
    {
        if (inputHandler != null)
        {
            inputHandler.OnInventoryPressed -= ToggleMenu;
        }
        RestoreGameplayState();
    }

    private void FindAndSubscribeInputHandler()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            inputHandler = playerObj.GetComponent<Player_InputHandler>();
            if (inputHandler == null)
                Debug.LogWarning("MenuController: Player_InputHandler not found on Player GameObject.");
            playerInput = playerObj.GetComponent<PlayerInput>();
            if (playerInput == null)
                Debug.LogWarning("MenuController: PlayerInput not found on Player GameObject.");
        }
        if (inputHandler == null)
        {
            inputHandler = FindFirstObjectByType<Player_InputHandler>();
            if (inputHandler == null)
                Debug.LogError("MenuController: Could not find Player_InputHandler in scene.");
        }
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput == null)
                Debug.LogError("MenuController: Could not find PlayerInput in scene.");
        }
        if (inputHandler != null)
        {
            inputHandler.OnInventoryPressed -= ToggleMenu; // Prevent double subscription
            inputHandler.OnInventoryPressed += ToggleMenu;
            Debug.Log("MenuController: Subscribed to OnInventoryPressed event.");
        }
    }

    private void DebugInventoryActionState(string context)
    {
        if (playerInput != null && playerInput.actions != null)
        {
            var playerMap = playerInput.actions.FindActionMap("Player", false);
            var uiMap = playerInput.actions.FindActionMap("UI", false);
            var invPlayer = playerMap?.FindAction("Inventory", false);
            var invUI = uiMap?.FindAction("Inventory", false);
            Debug.Log($"[InputDebug] {context}: PlayerMap.Inventory enabled={invPlayer?.enabled}, UIMap.Inventory enabled={invUI?.enabled}");
        }
    }

    private void EnableInventoryActionInAllMaps()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError("MenuController: playerInput or playerInput.actions is null in EnableInventoryActionInAllMaps.");
            return;
        }
        var playerMap = playerInput.actions.FindActionMap("Player", false);
        var uiMap = playerInput.actions.FindActionMap("UI", false);
        if (playerMap == null)
            Debug.LogError("MenuController: Player action map not found.");
        if (uiMap == null)
            Debug.LogError("MenuController: UI action map not found.");
        if (playerMap != null)
        {
            var inv = playerMap.FindAction("Inventory", false);
            if (inv == null)
                Debug.LogError("MenuController: Inventory action not found in Player map.");
            else if (!inv.enabled) inv.Enable();
        }
        if (uiMap != null)
        {
            var inv = uiMap.FindAction("Inventory", false);
            if (inv == null)
                Debug.LogError("MenuController: Inventory action not found in UI map.");
            else if (!inv.enabled) inv.Enable();
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
        // Mark tabs as navigation targets so they remain reachable
        EnsureNavTarget(inventoryTab != null ? inventoryTab.gameObject : null);
        EnsureNavTarget(equipmentTab != null ? equipmentTab.gameObject : null);
        EnsureNavTarget(mapTab != null ? mapTab.gameObject : null);
        EnsureSelectionHighlight(defaultEquipmentFocus);
    EnsureSelectionHighlight(defaultMapFocus);
        // Ensure default focus objects are also nav targets
        EnsureNavTarget(defaultEquipmentFocus);
        EnsureNavTarget(defaultMapFocus);
        // Ensure header tabs have symmetric left/right navigation
        WireHeaderHorizontalNavigation();
    // no gadgets tab
    }

    private void Update()
    {
        // Detect current input mode (mouse vs controller/keyboard) each frame while menu is open
        if (menuRoot != null && menuRoot.activeInHierarchy)
        {
            UIInputMode.DetectThisFrame();
            // Fallback: if a header control is selected and the user presses Down, force focus into page content
            HandleHeaderToContentFallback();
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
                            footerText.text = $"{item.itemName}{qtyStr}  E{item.description}";
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

    private void LateUpdate()
    {
        if (!pauseOnOpen) return;

        if (IsMenuOpen)
        {
            if (!Mathf.Approximately(Time.timeScale, 0f))
            {
                Time.timeScale = 0f;
            }
        }
        else
        {
            if (!Mathf.Approximately(Time.timeScale, 1f))
            {
                Time.timeScale = 1f;
            }
        }
    }

    private void HandleHeaderToContentFallback()
    {
        // Detect a downward navigation intent
        bool down = false;
        // Legacy keys
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) down = true;
#if ENABLE_INPUT_SYSTEM
        var gp = UnityEngine.InputSystem.Gamepad.current;
        if (gp != null)
        {
            if (gp.dpad.down.wasPressedThisFrame) down = true;
            var v = gp.leftStick.ReadValue();
            if (!down && v.y < -0.6f) down = true;
        }
#endif
        if (!down) return;

        if (es == null) es = EventSystem.current;
        if (es == null) return;

        var sel = es.currentSelectedGameObject;
        var activePage = (currentTabIndex == 0) ? inventoryPageGO : (currentTabIndex == 1) ? equipmentPageGO : mapPageGO;
        if (activePage == null) return;

        // If selection is not under the active page (i.e., header or null), send focus to the first selectable in the page
        if (sel == null || !IsChildOf(sel.transform, activePage.transform))
        {
            var first = GetFirstSelectableForCurrentPage();
            if (first != null)
            {
                es.SetSelectedGameObject(first);
            }
        }
    }

    private bool IsChildOf(Transform t, Transform parent)
    {
        if (t == null || parent == null) return false;
        var cur = t;
        while (cur != null)
        {
            if (cur == parent) return true;
            cur = cur.parent;
        }
        return false;
    }

    private GameObject GetFirstSelectableForCurrentPage()
    {
        switch (currentTabIndex)
        {
            case 0:
                if (inventoryPage != null)
                {
                    var g = inventoryPage.GetFirstSelectable();
                    if (g != null) return g;
                }
                return inventoryTab != null ? inventoryTab.gameObject : null;
            case 1:
                if (equipmentPage != null)
                {
                    var g = equipmentPage.GetFirstSelectable();
                    if (g != null) return g;
                }
                return defaultEquipmentFocus != null ? defaultEquipmentFocus : (equipmentTab != null ? equipmentTab.gameObject : null);
            case 2:
                return defaultMapFocus != null ? defaultMapFocus : (mapTab != null ? mapTab.gameObject : null);
        }
        return null;
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
        var guard = menuRoot.GetComponent<UISelectionGuard>();
        if (guard == null) guard = menuRoot.AddComponent<UISelectionGuard>();
        // Switch to UI input map
        if (playerInput != null)
        {
            try { playerInput.SwitchCurrentActionMap("UI"); } catch { }
        }
        EnableUIShortcuts();
        EnableInventoryActionInAllMaps();
        EnsureHighlightsForAllInteractables();
        if (es == null) es = EventSystem.current;
        if (es != null) es.sendNavigationEvents = true;
        var cycler = menuRoot.GetComponent<UINavForceCycle>();
        if (cycler != null) cycler.enabled = false;
        var rootScope = menuRoot.GetComponent<UINavScope>();
        if (rootScope != null) rootScope.enabled = false;
        ShowPage(currentTabIndex);
    }

    public void CloseMenu()
    {
        if (menuRoot == null) return;
        if (pauseOnOpen) Time.timeScale = 1f;
        if (es != null) es.SetSelectedGameObject(null);
        menuRoot.SetActive(false);
        // Switch back to Player input map
        if (playerInput != null)
        {
            try { playerInput.SwitchCurrentActionMap("Player"); } catch { }
        }
        DisableUIShortcuts();
        EnableInventoryActionInAllMaps();
        DebugInventoryActionState("CloseMenu");
        if (es == null) es = EventSystem.current;
        if (es != null) es.sendNavigationEvents = true;
    }

    private void OnDestroy()
    {
        RestoreGameplayState();
        DisableUIShortcuts();
        if (inputHandler != null)
        {
            inputHandler.OnInventoryPressed -= ToggleMenu;
        }
        if (cachedUIInventory != null)
        {
            cachedUIInventory.performed -= OnUIInventoryPerformed;
        }
        if (cachedUICancel != null)
        {
            cachedUICancel.performed -= OnUICancelPerformed;
        }
        if (cachedUILeftTab != null)
        {
            cachedUILeftTab.performed -= OnUILeftTabPerformed;
        }
        if (cachedUIRightTab != null)
        {
            cachedUIRightTab.performed -= OnUIRightTabPerformed;
        }
    }

    private void RestoreGameplayState()
    {
        if (pauseOnOpen)
        {
            Time.timeScale = 1f;
        }
        // Input maps stay enabled at all times, nothing else required.
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
            // Do NOT disable cachedUIInventory; keep it enabled at all times
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
            // First close any open context menu; only close the inventory if no menu was open
            bool closedContext = InventoryContextMenu.TryHideOpenMenu();
            if (!closedContext)
            {
                CloseMenu();
            }
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
        // Clear selection before toggling pages to avoid stale selection/highlight
        if (es == null) es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);
        
        if (inventoryPageGO != null) inventoryPageGO.SetActive(currentTabIndex == 0);
        if (equipmentPageGO != null) equipmentPageGO.SetActive(currentTabIndex == 1);
        if (mapPageGO != null) mapPageGO.SetActive(currentTabIndex == 2);
        // Ensure any page/root scopes are disabled to let Unity's directional navigation work
        DisablePageScope(inventoryPageGO);
        DisablePageScope(equipmentPageGO);
        DisablePageScope(mapPageGO);
        var rootScope = menuRoot != null ? menuRoot.GetComponent<UINavScope>() : null;
        if (rootScope != null) rootScope.enabled = false;

        // Focus
        if (es == null) es = EventSystem.current;
        GameObject focus = null;
        switch (currentTabIndex)
        {
            case 0:
                if (inventoryPage != null) inventoryPage.Refresh();
                focus = inventoryPage != null ? inventoryPage.GetFirstSelectable() : (inventoryTab != null ? inventoryTab.gameObject : null);
                if (inventoryTab != null && focus != null)
                {
                    EnsureExplicitDown(inventoryTab, focus);
                }
                break;
            case 1:
                if (equipmentPage != null) equipmentPage.Refresh();
                focus = defaultEquipmentFocus != null ? defaultEquipmentFocus 
                    : (equipmentPage != null ? equipmentPage.GetFirstSelectable() 
                    : (equipmentTab != null ? equipmentTab.gameObject : null));
                if (equipmentTab != null && focus != null)
                {
                    EnsureExplicitDown(equipmentTab, focus);
                }
                break;
            case 2:
                focus = defaultMapFocus != null ? defaultMapFocus : (mapTab != null ? mapTab.gameObject : null);
                if (mapTab != null && focus != null)
                {
                    EnsureExplicitDown(mapTab, focus);
                }
                break;
        }
        // After setting explicit Down on the active tab, (re)wire header left/right so Explicit mode doesn't break L/R
        WireHeaderHorizontalNavigation();
        if (es != null && focus != null)
        {
            es.SetSelectedGameObject(focus);
        }
    }

    private void DisablePageScope(GameObject pageGO)
    {
        if (pageGO == null) return;
        var scope = pageGO.GetComponent<UINavScope>();
        if (scope != null) scope.enabled = false;
    }

    private void EnsureNavTarget(GameObject go)
    {
        if (go == null) return;
        if (go.GetComponent<UINavTarget>() == null) go.AddComponent<UINavTarget>();
    }

    private void EnsureExplicitDown(Selectable from, GameObject toGO)
    {
        if (from == null || toGO == null) return;
        var nav = from.navigation;
        nav.mode = Navigation.Mode.Explicit;
        var to = toGO.GetComponent<Selectable>();
        if (to == null)
        {
            to = toGO.GetComponentInChildren<Selectable>();
        }
        nav.selectOnDown = to;
        from.navigation = nav;
    }

    /// <summary>
    /// Ensures the header tab buttons have explicit left/right links so navigation is symmetric
    /// even when we switch nav.mode to Explicit to force Down behavior.
    /// </summary>
    private void WireHeaderHorizontalNavigation()
    {
        // Build ordered list of existing tabs
        var tabsList = new System.Collections.Generic.List<Selectable>();
        if (inventoryTab != null) tabsList.Add(inventoryTab);
        if (equipmentTab != null) tabsList.Add(equipmentTab);
        if (mapTab != null) tabsList.Add(mapTab);
        int n = tabsList.Count;
        for (int i = 0; i < n; i++)
        {
            var s = tabsList[i];
            var left = (i - 1) >= 0 ? tabsList[i - 1] : null;
            var right = (i + 1) < n ? tabsList[i + 1] : null;
            // Preserve existing Up/Down; only set L/R and ensure mode is Explicit
            var nav = s.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = left;
            nav.selectOnRight = right;
            s.navigation = nav;
        }
    }
}

