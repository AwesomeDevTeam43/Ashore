using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// Handles input rebinding UI and persistence using Unity Input System.
/// Includes conflict detection and confirmation dialog.
/// </summary>
public class ControlsRebindController
{
    private InputActionAsset inputActions;
    private InputActionRebindingExtensions.RebindingOperation currentRebindOperation;
    private Dictionary<string, TextMeshProUGUI> rebindButtonTexts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, Button> rebindButtons = new Dictionary<string, Button>();
    private List<Button> controlsTabButtons = new List<Button>();
    
    // Conflict dialog
    private GameObject conflictDialogPanel;
    private TextMeshProUGUI conflictMessageText;
    private Action onConflictConfirm;
    private Action onConflictCancel;
    private string pendingNewBindingPath;
    private InputAction pendingAction;
    private int pendingBindingIndex;
    private string pendingButtonId;
    private bool pendingIsKeyboard;
    
    public List<Button> ControlsTabButtons => controlsTabButtons;

    public ControlsRebindController(InputActionAsset actions)
    {
        inputActions = actions;
    }

    public void LoadInputRebinds()
    {
        if (inputActions == null) return;
        
        string rebinds = PlayerPrefs.GetString("InputRebinds", string.Empty);
        if (!string.IsNullOrEmpty(rebinds))
        {
            inputActions.LoadBindingOverridesFromJson(rebinds);
            Debug.Log("[ControlsRebindController] Loaded saved input rebinds");
        }
    }

    public GameObject CreateControlsTabContent(Transform parent, System.Action onReset)
    {
        controlsTabButtons.Clear();
        rebindButtonTexts.Clear();
        rebindButtons.Clear();
        
        GameObject content = new GameObject("ControlsContent");
        content.transform.SetParent(parent, false);
        
        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // Header row for Keyboard and Gamepad columns
        CreateControlsHeader(content.transform);

        // Create rebind rows for Input System actions
        float yPos = 0.82f;
        float yStep = 0.115f;
        
        CreateDualRebindRow(content.transform, "Move Up", "Move", "up", yPos);
        yPos -= yStep;
        CreateDualRebindRow(content.transform, "Move Down", "Move", "down", yPos);
        yPos -= yStep;
        CreateDualRebindRow(content.transform, "Move Left", "Move", "left", yPos);
        yPos -= yStep;
        CreateDualRebindRow(content.transform, "Move Right", "Move", "right", yPos);
        yPos -= yStep;
        CreateDualRebindRow(content.transform, "Jump", "Jump", "", yPos);
        yPos -= yStep;
        CreateDualRebindRow(content.transform, "Interact", "Interact", "", yPos);
        yPos -= yStep;
        CreateDualRebindRow(content.transform, "Attack", "Attack", "", yPos);

        // Reset to Defaults button
        PauseMenuUIFactory.CreateSmallButton(content.transform, "Reset to Defaults", 
            new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.1f), () => onReset?.Invoke());

        SetupRebindButtonNavigation();
        
        // Create conflict dialog (hidden by default)
        CreateConflictDialog(content.transform);

        return content;
    }

    #region Conflict Dialog
    
    private void CreateConflictDialog(Transform parent)
    {
        // Create overlay panel
        conflictDialogPanel = new GameObject("ConflictDialog");
        conflictDialogPanel.transform.SetParent(parent, false);
        
        RectTransform panelRect = conflictDialogPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        // Dark overlay background
        Image overlayBg = conflictDialogPanel.AddComponent<Image>();
        overlayBg.color = new Color(0, 0, 0, 0.9f);
        overlayBg.raycastTarget = true;
        
        // Dialog box
        GameObject dialogBox = new GameObject("DialogBox");
        dialogBox.transform.SetParent(conflictDialogPanel.transform, false);
        
        RectTransform dialogRect = dialogBox.AddComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.15f, 0.25f);
        dialogRect.anchorMax = new Vector2(0.85f, 0.75f);
        dialogRect.sizeDelta = Vector2.zero;
        
        Image dialogBg = dialogBox.AddComponent<Image>();
        dialogBg.color = new Color(0.12f, 0.15f, 0.2f, 1f);
        
        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(dialogBox.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.75f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "⚠ Conflito de Tecla";
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.3f);
        titleText.alignment = TextAlignmentOptions.Center;
        
        // Message
        GameObject messageGO = new GameObject("Message");
        messageGO.transform.SetParent(dialogBox.transform, false);
        RectTransform messageRect = messageGO.AddComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.08f, 0.35f);
        messageRect.anchorMax = new Vector2(0.92f, 0.72f);
        messageRect.sizeDelta = Vector2.zero;
        
        conflictMessageText = messageGO.AddComponent<TextMeshProUGUI>();
        conflictMessageText.text = "";
        conflictMessageText.fontSize = 26;
        conflictMessageText.color = Color.white;
        conflictMessageText.alignment = TextAlignmentOptions.Center;
        conflictMessageText.textWrappingMode = TextWrappingModes.Normal;
        
        // Confirm button
        GameObject confirmBtnGO = new GameObject("ConfirmButton");
        confirmBtnGO.transform.SetParent(dialogBox.transform, false);
        
        RectTransform confirmRect = confirmBtnGO.AddComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.1f, 0.08f);
        confirmRect.anchorMax = new Vector2(0.45f, 0.25f);
        confirmRect.sizeDelta = Vector2.zero;
        
        Image confirmImg = confirmBtnGO.AddComponent<Image>();
        confirmImg.color = new Color(0.2f, 0.7f, 0.3f, 1f);
        
        Button confirmBtn = confirmBtnGO.AddComponent<Button>();
        ColorBlock confirmColors = confirmBtn.colors;
        confirmColors.normalColor = new Color(0.2f, 0.7f, 0.3f, 1f);
        confirmColors.highlightedColor = new Color(0.3f, 0.85f, 0.4f, 1f);
        confirmColors.pressedColor = new Color(0.15f, 0.55f, 0.25f, 1f);
        confirmColors.selectedColor = new Color(0.3f, 0.85f, 0.4f, 1f);
        confirmBtn.colors = confirmColors;
        confirmBtn.onClick.AddListener(OnConflictConfirmClicked);
        
        confirmBtnGO.AddComponent<SelectionHighlight>();
        
        GameObject confirmTextGO = new GameObject("Text");
        confirmTextGO.transform.SetParent(confirmBtnGO.transform, false);
        RectTransform confirmTextRect = confirmTextGO.AddComponent<RectTransform>();
        confirmTextRect.anchorMin = Vector2.zero;
        confirmTextRect.anchorMax = Vector2.one;
        confirmTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI confirmText = confirmTextGO.AddComponent<TextMeshProUGUI>();
        confirmText.text = "Sim, Mudar";
        confirmText.fontSize = 28;
        confirmText.color = Color.white;
        confirmText.alignment = TextAlignmentOptions.Center;
        
        // Cancel button
        GameObject cancelBtnGO = new GameObject("CancelButton");
        cancelBtnGO.transform.SetParent(dialogBox.transform, false);
        
        RectTransform cancelRect = cancelBtnGO.AddComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.55f, 0.08f);
        cancelRect.anchorMax = new Vector2(0.9f, 0.25f);
        cancelRect.sizeDelta = Vector2.zero;
        
        Image cancelImg = cancelBtnGO.AddComponent<Image>();
        cancelImg.color = new Color(0.7f, 0.25f, 0.25f, 1f);
        
        Button cancelBtn = cancelBtnGO.AddComponent<Button>();
        ColorBlock cancelColors = cancelBtn.colors;
        cancelColors.normalColor = new Color(0.7f, 0.25f, 0.25f, 1f);
        cancelColors.highlightedColor = new Color(0.85f, 0.35f, 0.35f, 1f);
        cancelColors.pressedColor = new Color(0.55f, 0.2f, 0.2f, 1f);
        cancelColors.selectedColor = new Color(0.85f, 0.35f, 0.35f, 1f);
        cancelBtn.colors = cancelColors;
        cancelBtn.onClick.AddListener(OnConflictCancelClicked);
        
        cancelBtnGO.AddComponent<SelectionHighlight>();
        
        GameObject cancelTextGO = new GameObject("Text");
        cancelTextGO.transform.SetParent(cancelBtnGO.transform, false);
        RectTransform cancelTextRect = cancelTextGO.AddComponent<RectTransform>();
        cancelTextRect.anchorMin = Vector2.zero;
        cancelTextRect.anchorMax = Vector2.one;
        cancelTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI cancelText = cancelTextGO.AddComponent<TextMeshProUGUI>();
        cancelText.text = "Cancelar";
        cancelText.fontSize = 28;
        cancelText.color = Color.white;
        cancelText.alignment = TextAlignmentOptions.Center;
        
        // Setup navigation between dialog buttons
        Navigation confirmNav = confirmBtn.navigation;
        confirmNav.mode = Navigation.Mode.Explicit;
        confirmNav.selectOnRight = cancelBtn;
        confirmBtn.navigation = confirmNav;
        
        Navigation cancelNav = cancelBtn.navigation;
        cancelNav.mode = Navigation.Mode.Explicit;
        cancelNav.selectOnLeft = confirmBtn;
        cancelBtn.navigation = cancelNav;
        
        // Hide initially
        conflictDialogPanel.SetActive(false);
    }
    
    private void ShowConflictDialog(string conflictingActionName, string newKey, 
        Action onConfirm, Action onCancel)
    {
        if (conflictDialogPanel == null) return;
        
        conflictMessageText.text = $"A tecla <color=#FFD700>{newKey}</color> já está atribuída a:\n\n" +
                                   $"<color=#87CEEB>{conflictingActionName}</color>\n\n" +
                                   $"Deseja substituir? A ação anterior ficará sem tecla atribuída.";
        
        onConflictConfirm = onConfirm;
        onConflictCancel = onCancel;
        
        conflictDialogPanel.SetActive(true);
        
        // Select confirm button
        var confirmBtn = conflictDialogPanel.transform.Find("DialogBox/ConfirmButton");
        if (confirmBtn != null)
        {
            EventSystem.current?.SetSelectedGameObject(confirmBtn.gameObject);
        }
    }
    
    private void HideConflictDialog()
    {
        if (conflictDialogPanel != null)
        {
            conflictDialogPanel.SetActive(false);
        }
        onConflictConfirm = null;
        onConflictCancel = null;
    }
    
    private void OnConflictConfirmClicked()
    {
        var callback = onConflictConfirm;
        HideConflictDialog();
        callback?.Invoke();
    }
    
    private void OnConflictCancelClicked()
    {
        var callback = onConflictCancel;
        HideConflictDialog();
        callback?.Invoke();
    }
    
    #endregion

    private void CreateControlsHeader(Transform parent)
    {
        int fontSize = PauseMenuUIFactory.ButtonFontSize;
        
        GameObject header = new GameObject("Header");
        header.transform.SetParent(parent, false);
        
        RectTransform headerRect = header.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.05f, 0.88f);
        headerRect.anchorMax = new Vector2(0.95f, 0.98f);
        headerRect.sizeDelta = Vector2.zero;

        // Action label
        GameObject actionLabel = new GameObject("ActionLabel");
        actionLabel.transform.SetParent(header.transform, false);
        RectTransform actionRect = actionLabel.AddComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0f, 0f);
        actionRect.anchorMax = new Vector2(0.35f, 1f);
        actionRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI actionTmp = actionLabel.AddComponent<TextMeshProUGUI>();
        actionTmp.text = "Action";
        actionTmp.fontSize = fontSize - 12;
        actionTmp.fontStyle = FontStyles.Bold;
        actionTmp.color = new Color(0.7f, 0.85f, 1f);
        actionTmp.alignment = TextAlignmentOptions.Left;

        // Keyboard label
        GameObject kbLabel = new GameObject("KeyboardLabel");
        kbLabel.transform.SetParent(header.transform, false);
        RectTransform kbRect = kbLabel.AddComponent<RectTransform>();
        kbRect.anchorMin = new Vector2(0.35f, 0f);
        kbRect.anchorMax = new Vector2(0.65f, 1f);
        kbRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI kbTmp = kbLabel.AddComponent<TextMeshProUGUI>();
        kbTmp.text = "⌨ Keyboard";
        kbTmp.fontSize = fontSize - 12;
        kbTmp.fontStyle = FontStyles.Bold;
        kbTmp.color = new Color(0.7f, 0.85f, 1f);
        kbTmp.alignment = TextAlignmentOptions.Center;

        // Gamepad label
        GameObject gpLabel = new GameObject("GamepadLabel");
        gpLabel.transform.SetParent(header.transform, false);
        RectTransform gpRect = gpLabel.AddComponent<RectTransform>();
        gpRect.anchorMin = new Vector2(0.65f, 0f);
        gpRect.anchorMax = new Vector2(1f, 1f);
        gpRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI gpTmp = gpLabel.AddComponent<TextMeshProUGUI>();
        gpTmp.text = "🎮 Gamepad";
        gpTmp.fontSize = fontSize - 12;
        gpTmp.fontStyle = FontStyles.Bold;
        gpTmp.color = new Color(0.7f, 0.85f, 1f);
        gpTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateDualRebindRow(Transform parent, string displayName, string actionName, 
        string compositePart, float yPosition)
    {
        int fontSize = PauseMenuUIFactory.ButtonFontSize;
        string rowId = string.IsNullOrEmpty(compositePart) ? actionName : $"{actionName}/{compositePart}";
        
        GameObject row = new GameObject("Row_" + rowId);
        row.transform.SetParent(parent, false);
        
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.05f, yPosition - 0.045f);
        rowRect.anchorMax = new Vector2(0.95f, yPosition + 0.045f);
        rowRect.sizeDelta = Vector2.zero;

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.35f, 1f);
        labelRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = displayName;
        labelTmp.fontSize = fontSize - 14;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.Left;

        // Keyboard rebind button
        CreateRebindButton(row.transform, rowId + "_KB", actionName, compositePart, 
            new Vector2(0.36f, 0.1f), new Vector2(0.64f, 0.9f), true);

        // Gamepad rebind button  
        CreateRebindButton(row.transform, rowId + "_GP", actionName, compositePart,
            new Vector2(0.66f, 0.1f), new Vector2(0.99f, 0.9f), false);
    }

    private void CreateRebindButton(Transform parent, string buttonId, string actionName, 
        string compositePart, Vector2 anchorMin, Vector2 anchorMax, bool isKeyboard)
    {
        int fontSize = PauseMenuUIFactory.ButtonFontSize;
        
        GameObject buttonGO = new GameObject("RebindButton_" + buttonId);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.sizeDelta = Vector2.zero;
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        
        Button button = buttonGO.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.25f, 0.25f, 0.3f, 1f);
        colors.highlightedColor = PauseMenuUIFactory.ButtonHoverColor;
        colors.pressedColor = PauseMenuUIFactory.ButtonColor;
        colors.selectedColor = PauseMenuUIFactory.ButtonHoverColor;
        button.colors = colors;
        
        // Set automatic navigation
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;
        
        // Add SelectionHighlight for selection feedback
        buttonGO.AddComponent<SelectionHighlight>();
        
        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI buttonTmp = textGO.AddComponent<TextMeshProUGUI>();
        buttonTmp.text = GetCurrentBindingDisplay(actionName, compositePart, isKeyboard);
        buttonTmp.fontSize = fontSize - 16;
        buttonTmp.color = Color.white;
        buttonTmp.alignment = TextAlignmentOptions.Center;
        
        // Store references
        rebindButtonTexts[buttonId] = buttonTmp;
        rebindButtons[buttonId] = button;
        
        // Capture for lambda
        string capturedActionName = actionName;
        string capturedCompositePart = compositePart;
        string capturedButtonId = buttonId;
        bool capturedIsKeyboard = isKeyboard;
        
        button.onClick.AddListener(() => StartInputSystemRebind(capturedActionName, capturedCompositePart, 
            capturedButtonId, capturedIsKeyboard));
    }

    private void SetupRebindButtonNavigation()
    {
        string[] rowIds = { "Move Up", "Move Down", "Move Left", "Move Right", "Jump", "Interact", "Attack" };
        
        controlsTabButtons.Clear();
        foreach (string rowId in rowIds)
        {
            string kbId = rowId + "_KB";
            string gpId = rowId + "_GP";
            
            if (rebindButtons.ContainsKey(kbId) && rebindButtons[kbId] != null)
            {
                Navigation nav = rebindButtons[kbId].navigation;
                nav.mode = Navigation.Mode.Automatic;
                rebindButtons[kbId].navigation = nav;
                controlsTabButtons.Add(rebindButtons[kbId]);
            }
            if (rebindButtons.ContainsKey(gpId) && rebindButtons[gpId] != null)
            {
                Navigation nav = rebindButtons[gpId].navigation;
                nav.mode = Navigation.Mode.Automatic;
                rebindButtons[gpId].navigation = nav;
                controlsTabButtons.Add(rebindButtons[gpId]);
            }
        }
    }

    private string GetCurrentBindingDisplay(string actionName, string compositePart, bool isKeyboard)
    {
        if (inputActions == null) return "???";
        
        var action = inputActions.FindAction(actionName);
        if (action == null) return "???";
        
        int bindingIndex = -1;
        string deviceFilter = isKeyboard ? "<Keyboard>" : "<Gamepad>";
        string altDeviceFilter = isKeyboard ? "<Mouse>" : null;
        
        if (!string.IsNullOrEmpty(compositePart))
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isPartOfComposite && 
                    action.bindings[i].name.Equals(compositePart, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Check both effectivePath and original path (for cleared bindings)
                    string effectivePath = action.bindings[i].effectivePath;
                    string originalPath = action.bindings[i].path;
                    
                    bool matchesEffective = !string.IsNullOrEmpty(effectivePath) && 
                        (effectivePath.Contains(deviceFilter) || (altDeviceFilter != null && effectivePath.Contains(altDeviceFilter)));
                    bool matchesOriginal = !string.IsNullOrEmpty(originalPath) && 
                        (originalPath.Contains(deviceFilter) || (altDeviceFilter != null && originalPath.Contains(altDeviceFilter)));
                    
                    if (matchesEffective || matchesOriginal)
                    {
                        bindingIndex = i;
                        break;
                    }
                }
            }
            
            if (bindingIndex < 0 && !isKeyboard)
            {
                return compositePart switch
                {
                    "up" => "L-Stick ↑",
                    "down" => "L-Stick ↓",
                    "left" => "L-Stick ←",
                    "right" => "L-Stick →",
                    _ => "L-Stick"
                };
            }
        }
        else
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite)
                {
                    // Check both effectivePath and original path (for cleared bindings)
                    string effectivePath = action.bindings[i].effectivePath;
                    string originalPath = action.bindings[i].path;
                    
                    bool matchesEffective = !string.IsNullOrEmpty(effectivePath) && 
                        (effectivePath.Contains(deviceFilter) || (altDeviceFilter != null && effectivePath.Contains(altDeviceFilter)));
                    bool matchesOriginal = !string.IsNullOrEmpty(originalPath) && 
                        (originalPath.Contains(deviceFilter) || (altDeviceFilter != null && originalPath.Contains(altDeviceFilter)));
                    
                    if (matchesEffective || matchesOriginal)
                    {
                        bindingIndex = i;
                        break;
                    }
                }
            }
        }
        
        if (bindingIndex < 0) return "---";
        
        // Check if binding is empty (was cleared due to conflict)
        string displayString = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        return string.IsNullOrEmpty(displayString) ? "---" : displayString;
    }

    private void StartInputSystemRebind(string actionName, string compositePart, string buttonId, bool isKeyboard)
    {
        if (inputActions == null)
        {
            Debug.LogError("[ControlsRebindController] No InputActionAsset found!");
            return;
        }
        
        var action = inputActions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"[ControlsRebindController] Action '{actionName}' not found!");
            return;
        }
        
        string deviceFilter = isKeyboard ? "<Keyboard>" : "<Gamepad>";
        string altDeviceFilter = isKeyboard ? "<Mouse>" : null;
        int bindingIndex = -1;
        
        if (!string.IsNullOrEmpty(compositePart))
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isPartOfComposite && 
                    action.bindings[i].name.Equals(compositePart, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Check effectivePath first, then fall back to original path
                    // This handles cases where binding was cleared (effectivePath is empty)
                    string effectivePath = action.bindings[i].effectivePath;
                    string originalPath = action.bindings[i].path;
                    
                    bool matchesEffective = !string.IsNullOrEmpty(effectivePath) && 
                        (effectivePath.Contains(deviceFilter) || (altDeviceFilter != null && effectivePath.Contains(altDeviceFilter)));
                    bool matchesOriginal = !string.IsNullOrEmpty(originalPath) && 
                        (originalPath.Contains(deviceFilter) || (altDeviceFilter != null && originalPath.Contains(altDeviceFilter)));
                    
                    if (matchesEffective || matchesOriginal)
                    {
                        bindingIndex = i;
                        break;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite)
                {
                    // Check effectivePath first, then fall back to original path
                    string effectivePath = action.bindings[i].effectivePath;
                    string originalPath = action.bindings[i].path;
                    
                    bool matchesEffective = !string.IsNullOrEmpty(effectivePath) && 
                        (effectivePath.Contains(deviceFilter) || (altDeviceFilter != null && effectivePath.Contains(altDeviceFilter)));
                    bool matchesOriginal = !string.IsNullOrEmpty(originalPath) && 
                        (originalPath.Contains(deviceFilter) || (altDeviceFilter != null && originalPath.Contains(altDeviceFilter)));
                    
                    if (matchesEffective || matchesOriginal)
                    {
                        bindingIndex = i;
                        break;
                    }
                }
            }
        }
        
        if (bindingIndex < 0)
        {
            Debug.LogWarning($"[ControlsRebindController] No {(isKeyboard ? "keyboard" : "gamepad")} binding found for {actionName}/{compositePart}");
            return;
        }
        
        // Update UI
        if (rebindButtonTexts.TryGetValue(buttonId, out var buttonText))
        {
            buttonText.text = isKeyboard ? "Press key..." : "Press button...";
        }
        if (rebindButtons.TryGetValue(buttonId, out var button))
        {
            button.GetComponent<Image>().color = PauseMenuUIFactory.ButtonColor;
        }
        
        currentRebindOperation?.Cancel();
        action.Disable();
        
        Debug.Log($"[ControlsRebindController] Starting rebind for {actionName}/{compositePart} ({(isKeyboard ? "Keyboard" : "Gamepad")}) at binding index {bindingIndex}");
        
        var rebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f);
        
        if (isKeyboard)
        {
            rebindOp = rebindOp.WithControlsExcluding("<Gamepad>");
        }
        else
        {
            rebindOp = rebindOp.WithControlsExcluding("<Keyboard>")
                               .WithControlsExcluding("<Mouse>");
        }
        
        string capturedButtonId = buttonId;
        bool capturedIsKeyboard = isKeyboard;
        
        currentRebindOperation = rebindOp
            .OnComplete(operation => CompleteInputSystemRebind(action, capturedButtonId, capturedIsKeyboard))
            .OnCancel(operation => CancelInputSystemRebind(action, capturedButtonId, capturedIsKeyboard))
            .Start();
    }

    private void CompleteInputSystemRebind(InputAction action, string buttonId, bool isKeyboard)
    {
        Debug.Log($"[ControlsRebindController] Rebind complete for {buttonId}");
        
        // Get the new binding path before checking for conflicts
        string rowId = buttonId.Replace("_KB", "").Replace("_GP", "");
        string[] parts = rowId.Split('/');
        string actionName = parts[0];
        string compositePart = parts.Length > 1 ? parts[1] : "";
        
        // Find which binding index was just set
        int reboundBindingIndex = FindBindingIndex(action, compositePart, isKeyboard);
        if (reboundBindingIndex < 0)
        {
            action.Enable();
            ResetButtonVisual(buttonId, actionName, compositePart, isKeyboard);
            currentRebindOperation?.Dispose();
            currentRebindOperation = null;
            return;
        }
        
        string newBindingPath = action.bindings[reboundBindingIndex].effectivePath;
        
        // Check for conflicts
        var conflict = FindConflictingBinding(action, reboundBindingIndex, newBindingPath, isKeyboard);
        
        if (conflict.HasValue)
        {
            // Store pending data for confirmation
            pendingNewBindingPath = newBindingPath;
            pendingAction = action;
            pendingBindingIndex = reboundBindingIndex;
            pendingButtonId = buttonId;
            pendingIsKeyboard = isKeyboard;
            
            string conflictDisplayName = GetActionDisplayName(conflict.Value.action, conflict.Value.bindingIndex);
            string keyDisplayName = action.GetBindingDisplayString(reboundBindingIndex, 
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
            
            // Revert the binding temporarily (we'll re-apply if confirmed)
            action.RemoveBindingOverride(reboundBindingIndex);
            
            ShowConflictDialog(conflictDisplayName, keyDisplayName,
                onConfirm: () => ApplyRebindWithConflictResolution(conflict.Value),
                onCancel: () => CancelConflictRebind()
            );
        }
        else
        {
            // No conflict - save normally
            FinalizeRebind(action, buttonId, actionName, compositePart, isKeyboard);
        }
    }
    
    private (InputAction action, int bindingIndex)? FindConflictingBinding(
        InputAction currentAction, int currentBindingIndex, string newPath, bool isKeyboard)
    {
        if (inputActions == null) return null;
        
        string deviceFilter = isKeyboard ? "<Keyboard>" : "<Gamepad>";
        string altDeviceFilter = isKeyboard ? "<Mouse>" : null;
        
        foreach (var actionMap in inputActions.actionMaps)
        {
            // Skip UI action map - WASD/navigation keys shouldn't trigger conflicts
            if (actionMap.name.Equals("UI", StringComparison.OrdinalIgnoreCase)) continue;
            
            foreach (var action in actionMap.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    // Skip the binding we just set
                    if (action == currentAction && i == currentBindingIndex) continue;
                    
                    // Skip composite headers
                    if (action.bindings[i].isComposite) continue;
                    
                    string existingPath = action.bindings[i].effectivePath;
                    
                    // Check if this binding is for the same device type
                    bool isRelevantDevice = existingPath.Contains(deviceFilter) || 
                        (altDeviceFilter != null && existingPath.Contains(altDeviceFilter));
                    
                    if (!isRelevantDevice) continue;
                    
                    // Check if paths match
                    if (existingPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[ControlsRebindController] Conflict found: {action.name} binding {i} already uses {newPath}");
                        return (action, i);
                    }
                }
            }
        }
        
        return null;
    }
    
    private string GetActionDisplayName(InputAction action, int bindingIndex)
    {
        string actionName = action.name;
        var binding = action.bindings[bindingIndex];
        
        if (binding.isPartOfComposite && !string.IsNullOrEmpty(binding.name))
        {
            // Format: "Move (Up)" or "Move (Left)"
            string partName = binding.name;
            partName = char.ToUpper(partName[0]) + partName.Substring(1);
            return $"{actionName} ({partName})";
        }
        
        return actionName;
    }
    
    private int FindBindingIndex(InputAction action, string compositePart, bool isKeyboard)
    {
        string deviceFilter = isKeyboard ? "<Keyboard>" : "<Gamepad>";
        string altDeviceFilter = isKeyboard ? "<Mouse>" : null;
        
        if (!string.IsNullOrEmpty(compositePart))
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isPartOfComposite && 
                    action.bindings[i].name.Equals(compositePart, StringComparison.OrdinalIgnoreCase))
                {
                    // Check both effectivePath and original path (for cleared bindings)
                    string effectivePath = action.bindings[i].effectivePath;
                    string originalPath = action.bindings[i].path;
                    
                    bool matchesEffective = !string.IsNullOrEmpty(effectivePath) && 
                        (effectivePath.Contains(deviceFilter) || (altDeviceFilter != null && effectivePath.Contains(altDeviceFilter)));
                    bool matchesOriginal = !string.IsNullOrEmpty(originalPath) && 
                        (originalPath.Contains(deviceFilter) || (altDeviceFilter != null && originalPath.Contains(altDeviceFilter)));
                    
                    if (matchesEffective || matchesOriginal)
                    {
                        return i;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite)
                {
                    // Check both effectivePath and original path (for cleared bindings)
                    string effectivePath = action.bindings[i].effectivePath;
                    string originalPath = action.bindings[i].path;
                    
                    bool matchesEffective = !string.IsNullOrEmpty(effectivePath) && 
                        (effectivePath.Contains(deviceFilter) || (altDeviceFilter != null && effectivePath.Contains(altDeviceFilter)));
                    bool matchesOriginal = !string.IsNullOrEmpty(originalPath) && 
                        (originalPath.Contains(deviceFilter) || (altDeviceFilter != null && originalPath.Contains(altDeviceFilter)));
                    
                    if (matchesEffective || matchesOriginal)
                    {
                        return i;
                    }
                }
            }
        }
        
        return -1;
    }
    
    private void ApplyRebindWithConflictResolution((InputAction action, int bindingIndex) conflict)
    {
        Debug.Log($"[ControlsRebindController] Resolving conflict - removing binding from {conflict.action.name}");
        
        // Remove the conflicting binding (set it to empty)
        conflict.action.ApplyBindingOverride(conflict.bindingIndex, "");
        
        // Re-apply our new binding
        pendingAction.ApplyBindingOverride(pendingBindingIndex, pendingNewBindingPath);
        
        // Save and finalize
        string rowId = pendingButtonId.Replace("_KB", "").Replace("_GP", "");
        string[] parts = rowId.Split('/');
        string actionName = parts[0];
        string compositePart = parts.Length > 1 ? parts[1] : "";
        
        FinalizeRebind(pendingAction, pendingButtonId, actionName, compositePart, pendingIsKeyboard);
        
        // Update ALL button texts to reflect changes (including the one we cleared)
        RefreshAllBindingDisplays();
        
        ClearPendingData();
    }
    
    private void CancelConflictRebind()
    {
        Debug.Log("[ControlsRebindController] Conflict resolution canceled");
        
        if (pendingAction != null)
        {
            pendingAction.Enable();
            
            string rowId = pendingButtonId.Replace("_KB", "").Replace("_GP", "");
            string[] parts = rowId.Split('/');
            string actionName = parts[0];
            string compositePart = parts.Length > 1 ? parts[1] : "";
            
            ResetButtonVisual(pendingButtonId, actionName, compositePart, pendingIsKeyboard);
        }
        
        currentRebindOperation?.Dispose();
        currentRebindOperation = null;
        
        ClearPendingData();
    }
    
    private void ClearPendingData()
    {
        pendingNewBindingPath = null;
        pendingAction = null;
        pendingBindingIndex = -1;
        pendingButtonId = null;
        pendingIsKeyboard = false;
    }
    
    private void FinalizeRebind(InputAction action, string buttonId, string actionName, 
        string compositePart, bool isKeyboard)
    {
        string rebinds = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputRebinds", rebinds);
        PlayerPrefs.Save();
        
        action.Enable();
        ResetButtonVisual(buttonId, actionName, compositePart, isKeyboard);
        
        currentRebindOperation?.Dispose();
        currentRebindOperation = null;
        
        Debug.Log("[ControlsRebindController] Rebind saved successfully");
    }
    
    private void ResetButtonVisual(string buttonId, string actionName, string compositePart, bool isKeyboard)
    {
        if (rebindButtonTexts.TryGetValue(buttonId, out var buttonText))
        {
            buttonText.text = GetCurrentBindingDisplay(actionName, compositePart, isKeyboard);
        }
        if (rebindButtons.TryGetValue(buttonId, out var button))
        {
            button.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);
        }
    }
    
    private void RefreshAllBindingDisplays()
    {
        // Refresh all button texts to show current bindings
        string[] actionNames = { "Move", "Move", "Move", "Move", "Jump", "Interact", "Attack" };
        string[] compositeParts = { "up", "down", "left", "right", "", "", "" };
        string[] displayNames = { "Move Up", "Move Down", "Move Left", "Move Right", "Jump", "Interact", "Attack" };
        
        for (int i = 0; i < actionNames.Length; i++)
        {
            string rowId = string.IsNullOrEmpty(compositeParts[i]) ? 
                actionNames[i] : $"{actionNames[i]}/{compositeParts[i]}";
            
            string kbId = rowId + "_KB";
            string gpId = rowId + "_GP";
            
            if (rebindButtonTexts.TryGetValue(kbId, out var kbText))
            {
                kbText.text = GetCurrentBindingDisplay(actionNames[i], compositeParts[i], true);
            }
            if (rebindButtonTexts.TryGetValue(gpId, out var gpText))
            {
                gpText.text = GetCurrentBindingDisplay(actionNames[i], compositeParts[i], false);
            }
        }
    }

    private void CancelInputSystemRebind(InputAction action, string buttonId, bool isKeyboard)
    {
        Debug.Log($"[ControlsRebindController] Rebind canceled for {buttonId}");
        
        action.Enable();
        
        string rowId = buttonId.Replace("_KB", "").Replace("_GP", "");
        string[] parts = rowId.Split('/');
        string actionName = parts[0];
        string compositePart = parts.Length > 1 ? parts[1] : "";
        
        if (rebindButtonTexts.TryGetValue(buttonId, out var buttonText))
        {
            buttonText.text = GetCurrentBindingDisplay(actionName, compositePart, isKeyboard);
        }
        if (rebindButtons.TryGetValue(buttonId, out var button))
        {
            button.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);
        }
        
        currentRebindOperation?.Dispose();
        currentRebindOperation = null;
    }

    public void ResetAllInputBindings(Transform optionsPanel, System.Func<Transform, GameObject> recreateControlsTab)
    {
        if (inputActions == null) return;
        
        foreach (var map in inputActions.actionMaps)
        {
            map.RemoveAllBindingOverrides();
        }
        
        PlayerPrefs.DeleteKey("InputRebinds");
        PlayerPrefs.Save();
        
        Debug.Log("[ControlsRebindController] All input bindings reset to defaults");
        
        // Request recreation of controls tab
        recreateControlsTab?.Invoke(optionsPanel);
    }

    public void Dispose()
    {
        currentRebindOperation?.Dispose();
        currentRebindOperation = null;
    }
}
