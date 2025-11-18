using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Lightweight, runtime-built context menu for inventory items.
// Place this on a Canvas (or any UI parent). Provide an optional MenuRoot; otherwise, a simple one is created at runtime.
public class InventoryContextMenu : MonoBehaviour
{
    [Header("Optional Prefab/Root")]
    [Tooltip("If assigned, this RectTransform will be used as the popup menu root. It may contain buttons named PrimaryButton and DropButton with a Text (TMP) child.")]
    [SerializeField] private RectTransform menuRoot;

    [Header("Styling")] 
    [SerializeField] private Vector2 defaultSize = new Vector2(180, 140);
    [SerializeField] private Vector2 buttonSize = new Vector2(160, 28);
    [SerializeField] private float buttonSpacing = 6f;
    [SerializeField] private Vector2 screenPadding = new Vector2(8, 8);

    private Button primaryBtn, dropBtn;
    private TextMeshProUGUI primaryLabel;
    private System.Action onClose;

    private ItemData currentItem;
    private InventorySlot currentSlot;
    private System.Collections.Generic.List<Selectable> _disabledOutsideSelectables = new System.Collections.Generic.List<Selectable>();
    private GameObject _prevSelected;
    private System.Collections.Generic.List<UINavScope> _suspendedScopes = new System.Collections.Generic.List<UINavScope>();
    private System.Collections.Generic.List<InventoryContextMenuNavigator> _disabledNavigators = new System.Collections.Generic.List<InventoryContextMenuNavigator>();
    // Track last shown menu so other systems (e.g., MenuController) can close it first on Cancel
    private static InventoryContextMenu s_LastShown;
    private static float s_BlockOpenUntilTime = 0f;
    public static void BlockOpenFor(float seconds)
    {
        float t = Time.unscaledTime + Mathf.Max(0f, seconds);
        if (t > s_BlockOpenUntilTime) s_BlockOpenUntilTime = t;
    }
    public static bool IsOpenBlocked()
    {
        return Time.unscaledTime < s_BlockOpenUntilTime;
    }
    public static bool TryHideOpenMenu()
    {
        if (s_LastShown != null)
        {
            var root = s_LastShown.menuRoot;
            if (root != null && root.gameObject.activeSelf)
            {
                s_LastShown.Hide();
                return true;
            }
        }
        return false;
    }
    // We no longer use a click-blocker; we detect mouse clicks and close if the click did NOT
    // land on an InventorySlot (item) or on this menu itself.

    private void Awake()
    {
        EnsureMenuBuilt();
        Hide();
    }

    private void OnDisable()
    {
        ForceRestoreState();
    }

    private void OnDestroy()
    {
        ForceRestoreState();
    }

    private void EnsureMenuBuilt()
    {
        if (menuRoot != null)
        {
            // Find buttons by name (optional)
            primaryBtn = FindButton(menuRoot, "PrimaryButton");
            dropBtn = FindButton(menuRoot, "DropButton");
            if (primaryBtn != null)
            {
                primaryLabel = primaryBtn.GetComponentInChildren<TextMeshProUGUI>();
            }
            WireButtonsBase();
            return;
        }

        // Build a minimal menu at runtime
        var rootGO = new GameObject("InventoryContextMenuRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootGO.transform.SetParent(transform, false);
        menuRoot = rootGO.GetComponent<RectTransform>();
        var bg = rootGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        menuRoot.sizeDelta = defaultSize;
        menuRoot.pivot = new Vector2(0f, 1f);

    // Vertical stack: Primary and Drop
    primaryBtn = CreateButton("PrimaryButton", out primaryLabel);
    SetTMP(primaryLabel, "Use");
    dropBtn = CreateButton("DropButton", out _);
    SetTMP(dropBtn.GetComponentInChildren<TextMeshProUGUI>(), "Drop");
    WireButtonsBase();

        LayoutButtons();
    }

    private void LayoutButtons()
    {
        var buttons = new[] { primaryBtn, dropBtn };
        float y = -buttonSpacing;
        foreach (var b in buttons)
        {
            if (b == null) continue;
            var rt = b.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = buttonSize;
            rt.anchoredPosition = new Vector2(0f, y);
            y -= (buttonSize.y + buttonSpacing);
        }
        // Resize root to fit
        float totalH = -y + buttonSpacing;
        menuRoot.sizeDelta = new Vector2(Mathf.Max(menuRoot.sizeDelta.x, buttonSize.x + buttonSpacing * 2f), totalH + buttonSpacing);
    }

    private Button CreateButton(string name, out TextMeshProUGUI label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(menuRoot, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.1f);
        var btn = go.GetComponent<Button>();

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        label = textGO.GetComponent<TextMeshProUGUI>();
        label.text = name; label.alignment = TextAlignmentOptions.Center; label.fontSize = 20f; label.raycastTarget = false;

        return btn;
    }

    private Button FindButton(RectTransform root, string name)
    {
        var t = root.Find(name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private void WireButtonsBase()
    {
        if (primaryBtn) { primaryBtn.onClick.RemoveAllListeners(); }
        if (dropBtn) { dropBtn.onClick.RemoveAllListeners(); dropBtn.onClick.AddListener(() => { DoDrop(); Hide(); }); }
    }

    public void ShowFor(InventorySlot slot, ItemData item, Vector2 screenPosition, System.Action onClosed = null)
    {
        if (IsOpenBlocked()) return;
        currentSlot = slot;
        currentItem = item;
        onClose = onClosed;
        EnsureMenuBuilt();

    // No blocker; we handle global mouse clicks in Update()

        // Configure primary button action and label
        bool isEquip = item is EquipmentData;
        bool isCraftable = item != null && item.isCraftable;
        if (primaryBtn)
        {
            primaryBtn.onClick.RemoveAllListeners();
            if (isCraftable)
            {
                SetTMP(primaryLabel, "Craft");
                primaryBtn.onClick.AddListener(() => { DoCraft(); Hide(); });
            }
            else if (isEquip)
            {
                SetTMP(primaryLabel, "Equip");
                primaryBtn.onClick.AddListener(() => { DoEquip(); Hide(); });
            }
            else
            {
                SetTMP(primaryLabel, "Use");
                primaryBtn.onClick.AddListener(() => { DoUse(); Hide(); });
            }
            primaryBtn.gameObject.SetActive(true);
        }
        if (dropBtn) dropBtn.gameObject.SetActive(true);

        // Position in screen space relative to our canvas
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out var local);
            menuRoot.anchoredPosition = local;
        }
        else
        {
            menuRoot.position = screenPosition;
        }

        ClampToScreen(canvas);
    menuRoot.gameObject.SetActive(true);
    s_LastShown = this;

    // Trap focus BEFORE choosing selection so EventSystem doesn't clear selection due to disabled previous object
    TrapFocus();
    ConfigureNavigation();
    SuspendNavScopes();
    SuspendMenuNavigators();

    // Ensure buttons are interactable
    if (primaryBtn != null) primaryBtn.interactable = true;
    if (dropBtn != null) dropBtn.interactable = true;

    // Focus first active button (primary preferred) AFTER trap
    var first = (primaryBtn != null && primaryBtn.gameObject.activeSelf) ? primaryBtn : dropBtn;
    if (first != null) EventSystem.current?.SetSelectedGameObject(first.gameObject);
    }

    public void Hide()
    {
        // Debounce re-open due to the same Submit/Enter press
        BlockOpenFor(0.12f);
        // If we're in keyboard/controller mode, restore selection to the previously selected slot
        if (UIInputMode.Current != UIInputMode.Mode.Pointer && currentSlot != null)
        {
            EventSystem.current?.SetSelectedGameObject(currentSlot.gameObject);
        }
        if (menuRoot != null) menuRoot.gameObject.SetActive(false);
        onClose?.Invoke();
        onClose = null;
        currentItem = null; currentSlot = null;
        if (s_LastShown == this) s_LastShown = null;
        RestoreFocus();
        RestoreNavScopes();
        RestoreMenuNavigators();
    }

    private void DoUse()
    {
        if (currentSlot != null) currentSlot.OnUseItem();
        // Ensure the menu closes even if button wiring changes
        Hide();
    }

    private void DoEquip()
    {
        if (currentItem is EquipmentData eq)
        {
            if (eq.isCraftable)
            {
                Debug.Log("Cannot equip: this equipment must be crafted first.");
                Hide();
                return;
            }
            // Route through EquipmentManager to respect rules
            if (EquipmentManager.instance != null)
            {
                EquipmentManager.instance.EquipFromInventory(eq);
            }
            Hide();
        }
    }

    private void DoCraft()
    {
        if (currentItem == null || !currentItem.isCraftable) return;
        var inv = Inventory.instance;
        if (inv == null)
        {
            Debug.LogWarning("InventoryContextMenu: Inventory instance not found");
            return;
        }
        // Check if player has required materials
        if (!currentItem.CanCraft(inv.GetItemQuantity))
        {
            Debug.Log("Cannot craft: missing materials for " + currentItem.itemName);
            Hide();
            return;
        }
        // Consume ingredients
        foreach (var ing in currentItem.craftIngredients)
        {
            if (ing == null || ing.material == null) continue;
            inv.Remove(ing.material, Mathf.Max(1, ing.amount));
        }
        // Optionally consume the selected item itself (acts like a blueprint/placeholder)
        if (currentItem.consumeOnCraft)
        {
            inv.Remove(currentItem, 1);
        }
        // Add result
        ItemData result = currentItem.craftResult != null ? currentItem.craftResult : currentItem;
        inv.Add(result, 1);
        Debug.Log("Crafted: " + result.itemName);
        Hide();
    }

    private void DoDrop()
    {
        if (currentItem == null) return;
        // Remove one from inventory
        if (Inventory.instance != null)
        {
            Inventory.instance.Remove(currentItem, 1);
        }
        // Spawn world object if equipment
        if (currentItem is EquipmentData eq && eq.equipmentPrefab != null)
        {
            Transform player = null;
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
            Vector3 pos = player != null ? player.position + new Vector3(0.6f, 0.6f, 0f) : Vector3.zero;
            Object.Instantiate(eq.equipmentPrefab, pos, Quaternion.identity);
        }
    }

    private void Update()
    {
        // Dismiss on Cancel (Esc/B) for convenience
        if (menuRoot != null && menuRoot.gameObject.activeSelf)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
            }
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if (gp != null && (gp.bButton.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame))
            {
                Hide();
            }
            // We intentionally do NOT auto close on outside clicks to enforce focus trap.

            // Manual directional nav fallback to guarantee Up/Down between menu buttons
            HandleManualDirectionalNav();

            // Guard: if selection was lost (e.g., due to device scheme change), reselect a menu button
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                var first = (primaryBtn != null && primaryBtn.gameObject.activeSelf) ? primaryBtn : dropBtn;
                if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }
    }

    private void HandleManualDirectionalNav()
    {
        var es = EventSystem.current; if (es == null) return;

        bool up = false, down = false;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            up |= kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
            down |= kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
        }
        var gp = UnityEngine.InputSystem.Gamepad.current;
        if (gp != null)
        {
            up |= gp.dpad.up.wasPressedThisFrame || gp.leftStick.up.wasPressedThisFrame;
            down |= gp.dpad.down.wasPressedThisFrame || gp.leftStick.down.wasPressedThisFrame;
        }

        if (!up && !down) return;

        var cur = es.currentSelectedGameObject;
        Button curBtn = null;
        if (cur != null)
        {
            curBtn = cur.GetComponent<Button>();
            if (curBtn == null)
            {
                curBtn = cur.GetComponentInParent<Button>();
            }
        }

        if (curBtn == null)
        {
            if (primaryBtn != null)
            {
                es.SetSelectedGameObject(primaryBtn.gameObject);
                curBtn = primaryBtn;
            }
            else if (dropBtn != null)
            {
                es.SetSelectedGameObject(dropBtn.gameObject);
                curBtn = dropBtn;
            }
        }

        if (curBtn == null) return;

        if (down)
        {
            if (curBtn == primaryBtn && dropBtn != null)
            {
                es.SetSelectedGameObject(dropBtn.gameObject);
                return;
            }
            if (curBtn == dropBtn && primaryBtn != null)
            {
                es.SetSelectedGameObject(primaryBtn.gameObject); // wrap
                return;
            }
        }
        else if (up)
        {
            if (curBtn == dropBtn && primaryBtn != null)
            {
                es.SetSelectedGameObject(primaryBtn.gameObject);
                return;
            }
            if (curBtn == primaryBtn && dropBtn != null)
            {
                es.SetSelectedGameObject(dropBtn.gameObject); // wrap reverse
                return;
            }
        }
    }

    private bool PointerHitsItemOrMenu(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        var ped = new PointerEventData(es) { position = screenPos };
        var results = new System.Collections.Generic.List<RaycastResult>();
        es.RaycastAll(ped, results);
        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            // If the click is on the menu itself, don't close
            if (menuRoot != null && r.gameObject.transform.IsChildOf(menuRoot)) return true;
            // If the click is on any inventory slot (item), don't close
            if (r.gameObject.GetComponentInParent<InventorySlot>() != null) return true;
        }
        return false;
    }

    private void ClampToScreen(Canvas canvas)
    {
        if (canvas == null || menuRoot == null) return;
        var rtCanvas = canvas.transform as RectTransform;
        var size = menuRoot.sizeDelta;
        var pos = menuRoot.anchoredPosition;
        var half = size * 0.5f;
        var min = -rtCanvas.rect.size * 0.5f + half + screenPadding;
        var max = rtCanvas.rect.size * 0.5f - half - screenPadding;
        pos.x = Mathf.Clamp(pos.x, min.x, max.x);
        pos.y = Mathf.Clamp(pos.y, min.y, max.y);
        menuRoot.anchoredPosition = pos;
    }

    private static void SetTMP(TextMeshProUGUI tmp, string text)
    {
        if (tmp == null) return;
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void TrapFocus()
    {
        _prevSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        _disabledOutsideSelectables.Clear();
        var all = GameObject.FindObjectsByType<Selectable>(FindObjectsSortMode.None);
        foreach (var sel in all)
        {
            if (sel == null) continue;
            if (menuRoot != null && sel.transform.IsChildOf(menuRoot)) continue; // keep menu buttons active
            if (!sel.interactable) continue; // already disabled elsewhere
            sel.interactable = false;
            _disabledOutsideSelectables.Add(sel);
        }
    }

    private void RestoreFocus()
    {
        foreach (var sel in _disabledOutsideSelectables)
        {
            if (sel != null) sel.interactable = true;
        }
        _disabledOutsideSelectables.Clear();
        // Restore previous selection if still valid and we're in pointer mode; controller mode already restores slot
        if (UIInputMode.Current == UIInputMode.Mode.Pointer && _prevSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_prevSelected);
        }
        _prevSelected = null;
    }

    private void ConfigureNavigation()
    {
        // Explicit vertical navigation between primary and drop buttons
        if (primaryBtn != null && dropBtn != null)
        {
            var navPrimary = primaryBtn.navigation;
            navPrimary.mode = Navigation.Mode.Explicit;
            navPrimary.selectOnDown = dropBtn;
            navPrimary.selectOnUp = dropBtn; // wrap
            primaryBtn.navigation = navPrimary;

            var navDrop = dropBtn.navigation;
            navDrop.mode = Navigation.Mode.Explicit;
            navDrop.selectOnUp = primaryBtn;
            navDrop.selectOnDown = primaryBtn; // wrap
            dropBtn.navigation = navDrop;
        }
        // Add highlight + nav target components if missing
        AddHighlightAndMarker(primaryBtn);
        AddHighlightAndMarker(dropBtn);
    }

    private void AddHighlightAndMarker(Button btn)
    {
        if (btn == null) return;
        if (btn.GetComponent<SelectionHighlight>() == null)
        {
            btn.gameObject.AddComponent<SelectionHighlight>();
        }
        if (btn.GetComponent<UINavTarget>() == null)
        {
            btn.gameObject.AddComponent<UINavTarget>();
        }
        // Improve visibility via colorBlock tweaks
        var cb = btn.colors;
        cb.normalColor = new Color(1f,1f,1f,0.15f);
        cb.highlightedColor = new Color(1f,0.85f,0.3f,0.55f);
        cb.selectedColor = new Color(1f,0.75f,0.2f,0.55f);
        cb.pressedColor = new Color(1f,0.6f,0.1f,0.6f);
        cb.disabledColor = new Color(1f,1f,1f,0.05f);
        btn.colors = cb;
    }

    private void SuspendNavScopes()
    {
        _suspendedScopes.Clear();
        // Find active scopes in ancestors (inventory page) and disable them so they don't override selection
        var scopes = GameObject.FindObjectsByType<UINavScope>(FindObjectsSortMode.None);
        foreach (var sc in scopes)
        {
            if (sc == null) continue;
            if (!sc.isActiveAndEnabled) continue;
            // Disable only if menuRoot is not under that scope (to avoid breaking slot nav inside menu)
            if (menuRoot != null && !menuRoot.IsChildOf(sc.transform))
            {
                sc.enabled = false;
                _suspendedScopes.Add(sc);
            }
        }
    }

    private void RestoreNavScopes()
    {
        foreach (var sc in _suspendedScopes)
        {
            if (sc != null) sc.enabled = true;
        }
        _suspendedScopes.Clear();
    }

    private void SuspendMenuNavigators()
    {
        _disabledNavigators.Clear();
        // Disable any InventoryContextMenuNavigator to avoid double-handling of inputs
        var navs = GameObject.FindObjectsByType<InventoryContextMenuNavigator>(FindObjectsSortMode.None);
        foreach (var n in navs)
        {
            if (n == null) continue;
            if (!n.isActiveAndEnabled) continue;
            n.enabled = false;
            _disabledNavigators.Add(n);
        }
    }

    private void RestoreMenuNavigators()
    {
        foreach (var n in _disabledNavigators)
        {
            if (n != null) n.enabled = true;
        }
        _disabledNavigators.Clear();
    }

    private void ForceRestoreState()
    {
        // When the menu root is disabled/destroyed (e.g., scene transition), ensure we undo any focus/nav overrides
        RestoreFocus();
        RestoreNavScopes();
        RestoreMenuNavigators();
        currentSlot = null;
        currentItem = null;
        if (s_LastShown == this) s_LastShown = null;
        if (menuRoot != null) menuRoot.gameObject.SetActive(false);
    }
}
