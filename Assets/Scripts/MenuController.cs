using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public static bool InventoryOpen { get; private set; } = false;
    private GameObject lastPlayerObj = null;
    private Player_InputHandler lastInputHandler = null;
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

    [Header("HUD Disable (Optional)")]
    [Tooltip("If enabled, hides/disables the player's HUD while this menu is open.")]
    [SerializeField] private bool disablePlayerHUDWhenMenuOpen = true;
    [Tooltip("Optional: assign a HUD root GameObject to SetActive(false) when menu opens.")]
    [SerializeField] private GameObject playerHUDRoot;
    [Tooltip("Optional: assign a CanvasGroup to fade/disable HUD when menu opens.")]
    [SerializeField] private CanvasGroup playerHUDCanvasGroup;

    private bool hudWasHidden = false;

    private static GameObject FindSceneObjectByName(string exactOrPrefix)
    {
        if (string.IsNullOrWhiteSpace(exactOrPrefix)) return null;

        // Includes inactive objects; works for both scene objects and disabled prefabs.
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (t == null) continue;
            var n = t.name;
            if (string.Equals(n, exactOrPrefix, System.StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith(exactOrPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return t.gameObject;
            }
        }
        return null;
    }

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
    private string currentScene;

    public bool IsMenuOpen => menuRoot != null && menuRoot.activeSelf;

    private void Awake()
    {
        es = EventSystem.current;
        if (menuRoot == null)
        {
            menuRoot = this.gameObject; // assume this script is on the MenuCanvas root
        }
        DontDestroyOnLoad(this.gameObject);
        currentScene = SceneManager.GetActiveScene().name;
        // Start with menu hidden
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
        RobustFindAndSubscribePlayer();
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
        // Ensure header tabs have symmetric left/right navigation
        WireHeaderHorizontalNavigation();
    // no gadgets tab
    }

    private void Update()
    {
        // Robustly re-find and re-subscribe to Player and InputHandler after scene change or player respawn
        RobustFindAndSubscribePlayer();

        // Detect current input mode (mouse vs controller/keyboard) each frame while menu is open
        if (menuRoot != null && menuRoot.activeInHierarchy)
        {
            UIInputMode.DetectThisFrame();
            HandleHeaderToContentFallback();
        }

        // Check for scene change and close menu if changed
        string activeScene = SceneManager.GetActiveScene().name;
        if (currentScene != activeScene)
        {
            Debug.Log($"[MenuController] Scene changed from {currentScene} to {activeScene}");
            currentScene = activeScene;
            if (menuRoot != null && menuRoot.activeSelf)
            {
                CloseMenu();
            }
            // Explicitly reset inventory/menu state after scene change
            InventoryOpen = false;
            if (menuRoot != null)
            {
                menuRoot.SetActive(false);
                Debug.Log("[MenuController] menuRoot.SetActive(false) after scene change");
            }
            Debug.Log($"[MenuController] After scene change: InventoryOpen={InventoryOpen}, menuRoot.activeSelf={(menuRoot != null ? menuRoot.activeSelf : (bool?)null)}");

            // Force re-enable UI shortcuts after scene change
            Debug.Log("[MenuController] Forcing EnableUIShortcuts after scene change");
            EnableUIShortcuts();

            // Log current inputHandler and playerInput state
            Debug.Log($"[MenuController] After scene change: inputHandler={inputHandler}, playerInput={playerInput}");
        }

        // No per-frame input map enforcement. Only enforce on menu open/close and inventory toggle.

        // Keyboard fallback for tab switching (optional): Q/E
        if (menuRoot != null && menuRoot.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Q)) PrevTab();
            if (Input.GetKeyDown(KeyCode.E)) NextTab();
            
            // Manual navigation handling as fallback if InputSystemUIInputModule isn't working
            HandleManualNavigation();

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

    // Robustly find and subscribe to the current Player and InputHandler, cleaning up old subscriptions
    private void RobustFindAndSubscribePlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != lastPlayerObj)
        {
            Debug.Log($"[MenuController] Player object changed. Old: {lastPlayerObj}, New: {playerObj}");
            // Unsubscribe from old handler
            if (lastInputHandler != null)
            {
                lastInputHandler.OnInventoryPressed -= ToggleMenu;
                Debug.Log("[MenuController] Unsubscribed from old Player_InputHandler.OnInventoryPressed");
            }
            inputHandler = null;
            playerInput = null;
            if (playerObj != null)
            {
                inputHandler = playerObj.GetComponent<Player_InputHandler>();
                playerInput = playerObj.GetComponent<PlayerInput>();
                Debug.Log($"[MenuController] Found new Player_InputHandler: {inputHandler}, PlayerInput: {playerInput}");
                if (inputHandler != null)
                {
                    inputHandler.OnInventoryPressed += ToggleMenu;
                    lastInputHandler = inputHandler;
                    Debug.Log("[MenuController] Subscribed to new Player_InputHandler.OnInventoryPressed");
                }
            }
            lastPlayerObj = playerObj;
        }
        // If playerObj is the same but inputHandler/playerInput are null (e.g. after reload), re-get
        if (playerObj != null && (inputHandler == null || playerInput == null))
        {
            inputHandler = playerObj.GetComponent<Player_InputHandler>();
            playerInput = playerObj.GetComponent<PlayerInput>();
            Debug.Log($"[MenuController] Re-fetched Player_InputHandler: {inputHandler}, PlayerInput: {playerInput}");
            if (inputHandler != null && lastInputHandler != inputHandler)
            {
                inputHandler.OnInventoryPressed += ToggleMenu;
                lastInputHandler = inputHandler;
                Debug.Log("[MenuController] Subscribed to Player_InputHandler.OnInventoryPressed after reload");
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
        Debug.Log($"[MenuController] ToggleMenu called. menuRoot.activeSelf={(menuRoot != null ? menuRoot.activeSelf : "null")}");
        if (menuRoot == null) return;
        
        // Don't open inventory if pause menu is open
        if (!menuRoot.activeSelf && GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused)
        {
            Debug.Log("[MenuController] Cannot open inventory - pause menu is active");
            return;
        }
        
        bool newState = !menuRoot.activeSelf;
        if (newState)
        {
            Debug.Log("[MenuController] Opening inventory menu");
            OpenMenu();
            InventoryOpen = true;
        }
        else
        {
            Debug.Log("[MenuController] Closing inventory menu");
            CloseMenu();
            InventoryOpen = false;
        }
        EnforceInputMap();
    }

    public void OpenMenu()
    {
        Debug.Log("[MenuController] OpenMenu called.");
        if (menuRoot == null) return;
        if (disablePlayerHUDWhenMenuOpen) SetPlayerHUDVisible(false);
        Debug.Log($"[MenuController] Opening menuRoot '{menuRoot.name}' (activeSelf before={menuRoot.activeSelf})");
        menuRoot.SetActive(true);
        Debug.Log($"[MenuController] menuRoot activeSelf after SetActive(true)={menuRoot.activeSelf}, activeInHierarchy={menuRoot.activeInHierarchy}");
        // Defensive UI visibility check: ensure Canvas/CanvasGroup aren't hiding the UI
        EnsureUIVisible(menuRoot);
        if (pauseOnOpen) { Debug.Log("[MenuController] Setting Time.timeScale = 0"); Time.timeScale = 0f; }
        // Enforce correct input map
        EnforceInputMap();
        EnableUIShortcuts();
        EnsureHighlightsForAllInteractables();
        // Re-enable built-in navigation
        if (es == null) es = EventSystem.current;
        if (es != null) es.sendNavigationEvents = true;
        // Ensure InputSystemUIInputModule's navigation actions are enabled
        EnsureUIModuleNavigationEnabled();
        // Ensure all menu selectables are interactable (in case a context menu trap didn't restore properly)
        EnsureMenuSelectablesInteractable();
        ShowPage(currentTabIndex);
    }

    public void CloseMenu()
    {
        Debug.Log("[MenuController] CloseMenu called.");
        if (menuRoot == null) return;
        // Only restore time scale if GamePauseManager isn't keeping the game paused
        if (pauseOnOpen) 
        { 
            bool gamePaused = GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused;
            if (!gamePaused)
            {
                Debug.Log("[MenuController] Setting Time.timeScale = 1"); 
                Time.timeScale = 1f; 
            }
            else
            {
                Debug.Log("[MenuController] Not restoring Time.timeScale - GamePauseManager is paused");
            }
        }
        if (es != null) es.SetSelectedGameObject(null);
        Debug.Log($"[MenuController] Deactivating menuRoot '{menuRoot.name}' (activeSelf before={menuRoot.activeSelf})");
        menuRoot.SetActive(false);
        Debug.Log($"[MenuController] menuRoot activeSelf after SetActive(false)={menuRoot.activeSelf}, activeInHierarchy={menuRoot.activeInHierarchy}");
        DisableUIShortcuts();
        InventoryOpen = false;
        if (disablePlayerHUDWhenMenuOpen) SetPlayerHUDVisible(true);
        // Enforce correct input map
        EnforceInputMap();
        // Ensure built-in navigation is on
        if (es == null) es = EventSystem.current;
        if (es != null) es.sendNavigationEvents = true;
    }

    private void ResolveHUDReferencesIfNeeded()
    {
        if (playerHUDRoot != null || playerHUDCanvasGroup != null) return;

        // First choice: a scene object explicitly named "Player UI" (common prefab/root name).
        var named = FindSceneObjectByName("Player UI");
        if (named != null)
        {
            // Don't pick something that also owns the menu.
            if (menuRoot == null || (named != menuRoot && !menuRoot.transform.IsChildOf(named.transform)))
            {
                playerHUDRoot = named;
                return;
            }
        }

        // Best-effort auto-wire: look for the persistent HUD root and its Manage_UI.
        var hudRoot = Object.FindFirstObjectByType<PersistentHUDRoot>();
        if (hudRoot == null) return;

        var manageUI = hudRoot.GetComponentInChildren<Manage_UI>(includeInactive: true);
        if (manageUI == null) return;

        // Prefer disabling a CanvasGroup if present on the Manage_UI object or parents.
        var cg = manageUI.GetComponentInParent<CanvasGroup>();
        if (cg != null)
        {
            playerHUDCanvasGroup = cg;
            return;
        }

        // Otherwise disable the Manage_UI container (or its parent if it won't hide the menuRoot).
        var candidate = manageUI.gameObject;
        var parent = manageUI.transform.parent;
        if (parent != null)
        {
            // Don't pick a parent that also contains menuRoot.
            if (menuRoot == null || !menuRoot.transform.IsChildOf(parent))
            {
                candidate = parent.gameObject;
            }
        }
        playerHUDRoot = candidate;
    }

    private void SetPlayerHUDVisible(bool visible)
    {
        ResolveHUDReferencesIfNeeded();

        if (!visible)
        {
            if (hudWasHidden) return;
            hudWasHidden = true;
        }
        else
        {
            hudWasHidden = false;
        }

        if (playerHUDCanvasGroup != null)
        {
            playerHUDCanvasGroup.alpha = visible ? 1f : 0f;
            playerHUDCanvasGroup.interactable = visible;
            playerHUDCanvasGroup.blocksRaycasts = visible;
            return;
        }

        if (playerHUDRoot != null)
        {
            // Don't accidentally hide the menu itself.
            if (menuRoot != null && (playerHUDRoot == menuRoot || menuRoot.transform.IsChildOf(playerHUDRoot.transform)))
            {
                return;
            }
            playerHUDRoot.SetActive(visible);
        }
    }

    private void EnforceInputMap()
    {
        if (playerInput == null)
        {
            Debug.LogWarning("[MenuController] EnforceInputMap: playerInput is null");
            return;
        }
        if (InventoryOpen || (menuRoot != null && menuRoot.activeSelf))
        {
            Debug.Log("[MenuController] EnforceInputMap: InventoryOpen or menuRoot.activeSelf, switching to UI map");
            try { playerInput.actions.FindActionMap("Player").Disable(); Debug.Log("[MenuController] Disabled Player map"); } catch (System.Exception ex) { Debug.LogWarning($"[MenuController] Failed to disable Player map: {ex.Message}"); }
            try { playerInput.actions.FindActionMap("UI").Enable(); Debug.Log("[MenuController] Enabled UI map"); } catch (System.Exception ex) { Debug.LogWarning($"[MenuController] Failed to enable UI map: {ex.Message}"); }
            try { playerInput.SwitchCurrentActionMap("UI"); Debug.Log("[MenuController] Switched to UI map"); } catch (System.Exception ex) { Debug.LogWarning($"[MenuController] Failed to switch to UI map: {ex.Message}"); }
        }
        else
        {
            Debug.Log("[MenuController] EnforceInputMap: Inventory closed, switching to Player map");
            try { playerInput.actions.FindActionMap("UI").Disable(); Debug.Log("[MenuController] Disabled UI map"); } catch (System.Exception ex) { Debug.LogWarning($"[MenuController] Failed to disable UI map: {ex.Message}"); }
            try { playerInput.actions.FindActionMap("Player").Enable(); Debug.Log("[MenuController] Enabled Player map"); } catch (System.Exception ex) { Debug.LogWarning($"[MenuController] Failed to enable Player map: {ex.Message}"); }
            try { playerInput.SwitchCurrentActionMap("Player"); Debug.Log("[MenuController] Switched to Player map"); } catch (System.Exception ex) { Debug.LogWarning($"[MenuController] Failed to switch to Player map: {ex.Message}"); }
        }
    }

    /// <summary>
    /// Ensures the InputSystemUIInputModule's navigation actions (move, submit, cancel) are enabled.
    /// This is crucial because the UI module uses its own action references (from DefaultInputActions),
    /// which are separate from the PlayerInput's action maps.
    /// </summary>
    private void EnsureUIModuleNavigationEnabled()
    {
        Debug.Log("[MenuController] EnsureUIModuleNavigationEnabled called");
        var uiModule = Object.FindFirstObjectByType<InputSystemUIInputModule>();
        if (uiModule == null)
        {
            Debug.LogWarning("[MenuController] EnsureUIModuleNavigationEnabled: No InputSystemUIInputModule found");
            return;
        }
        Debug.Log($"[MenuController] Found InputSystemUIInputModule on '{uiModule.gameObject.name}'");
        
        try
        {
            // Enable move action for navigation
            var moveAction = uiModule.move?.action;
            if (moveAction != null)
            {
                if (!moveAction.enabled)
                {
                    moveAction.Enable();
                    Debug.Log("[MenuController] Enabled UI move action");
                }
                else
                {
                    Debug.Log($"[MenuController] UI move action already enabled: {moveAction.name}");
                }
            }
            else
            {
                Debug.LogWarning("[MenuController] UI move action is null");
            }
            
            // Enable submit action
            var submitAction = uiModule.submit?.action;
            if (submitAction != null && !submitAction.enabled)
            {
                submitAction.Enable();
                Debug.Log("[MenuController] Enabled UI submit action");
            }
            
            // Enable cancel action
            var cancelAction = uiModule.cancel?.action;
            if (cancelAction != null && !cancelAction.enabled)
            {
                cancelAction.Enable();
                Debug.Log("[MenuController] Enabled UI cancel action");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MenuController] EnsureUIModuleNavigationEnabled failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures all Selectable components under the menu root are interactable.
    /// This recovers from situations where a context menu or other system disabled selectables
    /// and didn't properly restore them.
    /// </summary>
    private void EnsureMenuSelectablesInteractable()
    {
        if (menuRoot == null) return;
        var selectables = menuRoot.GetComponentsInChildren<Selectable>(true);
        foreach (var sel in selectables)
        {
            if (sel == null) continue;
            // Only re-enable if it's not intentionally disabled (e.g., empty equipment slot)
            // We check if navigation mode is not None, which typically indicates it should be navigable
            if (!sel.interactable && sel.navigation.mode != Navigation.Mode.None)
            {
                sel.interactable = true;
            }
        }
    }

    // Cooldown to prevent navigation repeating too fast
    private float _navCooldown = 0f;
    private const float NAV_REPEAT_DELAY = 0.15f;
    
    // State tracking for submit buttons to work when Time.timeScale = 0
    private bool _prevEnterPressed = false;
    private bool _prevNumpadEnterPressed = false;
    private bool _prevSpacePressed = false;
    private bool _prevGamepadSouthPressed = false;

    /// <summary>
    /// Manual navigation handling as fallback when InputSystemUIInputModule doesn't work.
    /// Reads keyboard/gamepad input directly and moves selection accordingly.
    /// </summary>
    private void HandleManualNavigation()
    {
        if (es == null) es = EventSystem.current;
        if (es == null) return;

        var current = es.currentSelectedGameObject;
        if (current == null) return;

        var sel = current.GetComponent<Selectable>();
        if (sel == null) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;
        
        // Handle submit with manual state tracking (works when Time.timeScale = 0)
        bool enterPressed = kb != null && kb.enterKey.isPressed;
        bool numpadEnterPressed = kb != null && kb.numpadEnterKey.isPressed;
        bool spacePressed = kb != null && kb.spaceKey.isPressed;
        bool gamepadSouthPressed = gp != null && gp.buttonSouth.isPressed;
        
        bool enterJustPressed = enterPressed && !_prevEnterPressed;
        bool numpadEnterJustPressed = numpadEnterPressed && !_prevNumpadEnterPressed;
        bool spaceJustPressed = spacePressed && !_prevSpacePressed;
        bool gamepadSouthJustPressed = gamepadSouthPressed && !_prevGamepadSouthPressed;
        
        _prevEnterPressed = enterPressed;
        _prevNumpadEnterPressed = numpadEnterPressed;
        _prevSpacePressed = spacePressed;
        _prevGamepadSouthPressed = gamepadSouthPressed;
        
        bool submit = enterJustPressed || numpadEnterJustPressed || spaceJustPressed || gamepadSouthJustPressed;
        
        if (submit && sel is Button btn)
        {
            Debug.Log($"[MenuController] Manual submit on: {current.name}");
            btn.onClick.Invoke();
            return;
        }

        // Respect cooldown for navigation (use unscaled time since game is paused)
        if (_navCooldown > 0f)
        {
            _navCooldown -= Time.unscaledDeltaTime;
            return;
        }

        // Read input from keyboard and gamepad
        Vector2 input = Vector2.zero;
        
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y = 1;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y = -1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x = -1;
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x = 1;
        }

        if (gp != null && input == Vector2.zero)
        {
            var stick = gp.leftStick.ReadValue();
            var dpad = gp.dpad.ReadValue();
            if (Mathf.Abs(stick.x) > 0.5f || Mathf.Abs(dpad.x) > 0.5f)
                input.x = stick.x > 0.5f || dpad.x > 0.5f ? 1 : -1;
            if (Mathf.Abs(stick.y) > 0.5f || Mathf.Abs(dpad.y) > 0.5f)
                input.y = stick.y > 0.5f || dpad.y > 0.5f ? 1 : -1;
        }

        if (input == Vector2.zero) return;

        Selectable next = null;
        if (input.y > 0) next = sel.FindSelectableOnUp();
        else if (input.y < 0) next = sel.FindSelectableOnDown();
        else if (input.x < 0) next = sel.FindSelectableOnLeft();
        else if (input.x > 0) next = sel.FindSelectableOnRight();

        if (next != null && next.gameObject != current)
        {
            Debug.Log($"[MenuController] Manual nav: {current.name} -> {next.gameObject.name}");
            es.SetSelectedGameObject(next.gameObject);
            _navCooldown = NAV_REPEAT_DELAY;
        }
        else if (next == null)
        {
            // Log when we can't find a next selectable - helps debug navigation setup
            Debug.Log($"[MenuController] Manual nav: No selectable found in direction {input} from {current.name}");
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
                focus = defaultEquipmentFocus != null ? defaultEquipmentFocus 
                    : (equipmentPage != null ? equipmentPage.GetFirstSelectable() 
                    : (equipmentTab != null ? equipmentTab.gameObject : null));
                break;
            case 2:
                focus = defaultMapFocus != null ? defaultMapFocus : (mapTab != null ? mapTab.gameObject : null);
                break;
        }
        // After setting explicit Down on the active tab, (re)wire header left/right so Explicit mode doesn't break L/R
        WireHeaderHorizontalNavigation();
        if (es != null && focus != null)
        {
            es.SetSelectedGameObject(focus);
            Debug.Log($"[MenuController] ShowPage: Set focus to '{focus.name}', interactable={focus.GetComponent<Selectable>()?.interactable}");
        }
        else
        {
            Debug.LogWarning($"[MenuController] ShowPage: Could not set focus. es={es}, focus={focus}");
        }
    }



    /// <summary>
    /// Ensures the header tab buttons have proper navigation.
    /// </summary>
    private void WireHeaderHorizontalNavigation()
    {
        // Use Automatic navigation - Unity handles horizontal layout well
        if (inventoryTab != null)
        {
            var nav = inventoryTab.navigation;
            nav.mode = Navigation.Mode.Automatic;
            inventoryTab.navigation = nav;
        }
        if (equipmentTab != null)
        {
            var nav = equipmentTab.navigation;
            nav.mode = Navigation.Mode.Automatic;
            equipmentTab.navigation = nav;
        }
        if (mapTab != null)
        {
            var nav = mapTab.navigation;
            nav.mode = Navigation.Mode.Automatic;
            mapTab.navigation = nav;
        }
    }

    // Inspect and fix common hidden UI issues (CanvasGroup alpha, disabled Canvas)
    private void EnsureUIVisible(GameObject root)
    {
        if (root == null) return;
        // Check Canvas components
        var canvas = root.GetComponentInChildren<Canvas>(includeInactive: true);
        if (canvas != null)
        {
            Debug.Log($"MenuController: Found Canvas '{canvas.name}' renderMode={canvas.renderMode} enabled={canvas.enabled} sortingOrder={canvas.sortingOrder}");
            if (!canvas.enabled)
            {
                Debug.LogWarning("MenuController: Canvas was disabled — enabling it.");
                canvas.enabled = true;
            }
        }

        // Check CanvasGroup
        var cg = root.GetComponentInChildren<CanvasGroup>(includeInactive: true);
        if (cg != null)
        {
            Debug.Log($"MenuController: Found CanvasGroup alpha={cg.alpha} interactable={cg.interactable} blocksRaycasts={cg.blocksRaycasts}");
            if (cg.alpha < 0.01f)
            {
                Debug.LogWarning("MenuController: CanvasGroup alpha was ~0 — forcing alpha=1 and enabling interactability.");
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        // RectTransform sanity
        var rt = root.GetComponent<RectTransform>();
        if (rt != null)
        {
            Debug.Log($"MenuController: Root RectTransform: anchoredPosition={rt.anchoredPosition}, sizeDelta={rt.sizeDelta}, scale={rt.localScale}");
            if (Mathf.Approximately(rt.localScale.x, 0f) || Mathf.Approximately(rt.localScale.y, 0f))
            {
                Debug.LogWarning("MenuController: Root scale was zero on one axis — resetting to Vector3.one.");
                rt.localScale = Vector3.one;
            }
        }

        // Ensure root itself is active in hierarchy
        Debug.Log($"MenuController: root.activeSelf={root.activeSelf}, root.activeInHierarchy={root.activeInHierarchy}");
    }
}