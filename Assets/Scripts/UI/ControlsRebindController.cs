using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles input rebinding UI and persistence using Unity Input System.
/// </summary>
public class ControlsRebindController
{
    private InputActionAsset inputActions;
    private InputActionRebindingExtensions.RebindingOperation currentRebindOperation;
    private Dictionary<string, TextMeshProUGUI> rebindButtonTexts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, Button> rebindButtons = new Dictionary<string, Button>();
    private List<Button> controlsTabButtons = new List<Button>();
    
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

        return content;
    }

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
        
        // Add UINavTarget and SelectionHighlight
        buttonGO.AddComponent<UINavTarget>();
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
                    string path = action.bindings[i].effectivePath;
                    if (path.Contains(deviceFilter) || (altDeviceFilter != null && path.Contains(altDeviceFilter)))
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
                    string path = action.bindings[i].effectivePath;
                    if (path.Contains(deviceFilter) || (altDeviceFilter != null && path.Contains(altDeviceFilter)))
                    {
                        bindingIndex = i;
                        break;
                    }
                }
            }
        }
        
        if (bindingIndex < 0) return "---";
        
        return action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
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
                    string path = action.bindings[i].effectivePath;
                    if (path.Contains(deviceFilter) || (altDeviceFilter != null && path.Contains(altDeviceFilter)))
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
                    string path = action.bindings[i].effectivePath;
                    if (path.Contains(deviceFilter) || (altDeviceFilter != null && path.Contains(altDeviceFilter)))
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
        
        string rebinds = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputRebinds", rebinds);
        PlayerPrefs.Save();
        
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
