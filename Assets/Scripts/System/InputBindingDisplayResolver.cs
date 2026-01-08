using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text.RegularExpressions;

[DefaultExecutionOrder(-200)]
public class InputBindingDisplayResolver : MonoBehaviour
{
    [Serializable]
    public class BindingAlias
    {
        [Tooltip("Token to search for in text (without braces). e.g. token 'Interact' matches '{Interact}'.")]
        public string token;
        [Tooltip("Input Action reference used to fetch the binding display string.")]
        public InputActionReference actionReference;
        [Tooltip("Fallback text when no binding is found or action is missing.")]
        public string fallback = "?";
        [Tooltip("Optional control scheme name to target for this token (e.g. 'Gamepad'). Leave empty to use the player's current scheme.")]
        public string controlSchemeOverride;
    }

    private static InputBindingDisplayResolver _instance;
    public static InputBindingDisplayResolver Instance => _instance;

    [SerializeField] private BindingAlias[] bindingAliases;
    [Tooltip("Optional explicit reference. Will auto-find a Player_InputHandler if left null.")]
    [SerializeField] private Player_InputHandler playerInputHandler;
    [Tooltip("Name of the control scheme to treat as default when nothing has been detected yet.")]
    [SerializeField] private string defaultControlScheme = "Keyboard&Mouse";

    private string _currentScheme;
    private Dictionary<string, BindingAlias> _aliasLookup = new Dictionary<string, BindingAlias>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, InputAction> _actionLookup;
    [SerializeField] private bool highlightBindingTokens = true;
    [SerializeField] private Color bindingHighlightColor = Color.yellow;
    private static readonly Regex TokenRegex = new Regex(@"\{([^{}]+)\}", RegexOptions.Compiled);

    public event Action OnBindingsChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        RebuildAliasLookup();
        TryResolvePlayerInputHandler();
        _currentScheme = defaultControlScheme;
    }

    private void OnEnable()
    {
        SubscribeToHandler();
    }

    private void OnDisable()
    {
        UnsubscribeFromHandler();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void TryResolvePlayerInputHandler()
    {
        if (playerInputHandler == null)
        {
            playerInputHandler = FindFirstObjectByType<Player_InputHandler>(FindObjectsInactive.Include);
            if (playerInputHandler != null)
            {
                BuildActionLookup(forceRebuild: true);
            }
        }
    }

    private void SubscribeToHandler()
    {
        TryResolvePlayerInputHandler();
        if (playerInputHandler != null)
        {
            playerInputHandler.OnControlSchemeChanged += HandleControlSchemeChanged;
            _currentScheme = playerInputHandler.CurrentControlScheme;
            BuildActionLookup(forceRebuild: true);
        }
    }

    private void UnsubscribeFromHandler()
    {
        if (playerInputHandler != null)
        {
            playerInputHandler.OnControlSchemeChanged -= HandleControlSchemeChanged;
        }
    }

    private void HandleControlSchemeChanged(string newScheme)
    {
        if (string.IsNullOrEmpty(newScheme)) return;
        if (!string.Equals(_currentScheme, newScheme, StringComparison.Ordinal))
        {
            _currentScheme = newScheme;
            OnBindingsChanged?.Invoke();
        }
    }

    public string FormatText(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        BuildActionLookup();
        return TokenRegex.Replace(template, match =>
        {
            string token = match.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(token)) return match.Value;
            string replacement = ResolveTokenReplacement(token);
            return string.IsNullOrEmpty(replacement) ? match.Value : ApplyBindingHighlight(replacement);
        });
    }

    private string ApplyBindingHighlight(string text)
    {
        if (!highlightBindingTokens || string.IsNullOrEmpty(text)) return text;
        var colorString = ColorUtility.ToHtmlStringRGBA(bindingHighlightColor);
        return $"<color=#{colorString}>{text}</color>";
    }

    private string ResolveTokenReplacement(string token)
    {
        BindingAlias alias = GetAlias(token);
        InputAction action = null;
        string schemeOverride = null;
        string fallbackText = token;

        if (alias != null)
        {
            if (!string.IsNullOrEmpty(alias.fallback)) fallbackText = alias.fallback;
            schemeOverride = alias.controlSchemeOverride;
            if (alias.actionReference != null)
            {
                action = alias.actionReference.action;
            }
        }

        if (action == null)
        {
            action = FindActionByToken(token);
        }

        if (action == null)
        {
            return fallbackText;
        }

        string schemeToUse = !string.IsNullOrEmpty(schemeOverride) ? schemeOverride : _currentScheme;
        string display = GetBindingDisplay(action, schemeToUse);
        if (string.IsNullOrEmpty(display) && !string.IsNullOrEmpty(defaultControlScheme))
        {
            display = GetBindingDisplay(action, defaultControlScheme);
        }
        if (string.IsNullOrEmpty(display))
        {
            display = fallbackText;
        }
        return display;
    }

    private string GetBindingDisplay(InputAction action, string scheme)
    {
        try
        {
            if (action == null) return null;
            // Prefer binding entries tagged with the active control scheme; fallback to default display string
            var bindingIndex = FindBindingIndexForScheme(action, scheme);
            if (bindingIndex >= 0)
            {
                return action.GetBindingDisplayString(bindingIndex);
            }
            return action.GetBindingDisplayString();
        }
        catch
        {
            return null;
        }
    }

    private int FindBindingIndexForScheme(InputAction action, string scheme)
    {
        if (action == null) return -1;
        var bindings = action.bindings;
        var schemeDefinition = ResolveSchemeDefinition(action, scheme);

        if (!string.IsNullOrEmpty(scheme))
        {
            int index = FindBindingIndexMatchingScheme(bindings, scheme, schemeDefinition);
            if (index >= 0)
            {
                return index;
            }
        }

        // fallback: prefer first composite root or standalone binding with a valid path
        for (int i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if (binding.isComposite && !string.IsNullOrEmpty(binding.path))
            {
                return i;
            }
            if (!binding.isPartOfComposite && !string.IsNullOrEmpty(binding.effectivePath))
            {
                return i;
            }
        }
        return -1;
    }

    private int FindBindingIndexMatchingScheme(UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputBinding> bindings, string scheme, InputControlScheme? schemeDefinition)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if (!BindingMatchesScheme(binding, scheme, schemeDefinition)) continue;

            if (binding.isPartOfComposite)
            {
                int compositeRoot = FindCompositeRootIndex(bindings, i);
                if (compositeRoot >= 0) return compositeRoot;
            }
            if (binding.isComposite)
            {
                return i;
            }
            if (!string.IsNullOrEmpty(binding.effectivePath))
            {
                return i;
            }
        }
        return -1;
    }

    private InputControlScheme? ResolveSchemeDefinition(InputAction action, string scheme)
    {
        if (action?.actionMap?.asset == null || string.IsNullOrEmpty(scheme)) return null;
        foreach (var sc in action.actionMap.asset.controlSchemes)
        {
            if (string.Equals(sc.name, scheme, StringComparison.OrdinalIgnoreCase))
            {
                return sc;
            }
        }
        return null;
    }

    private bool BindingMatchesScheme(InputBinding binding, string scheme, InputControlScheme? schemeDefinition)
    {
        if (string.IsNullOrEmpty(scheme)) return false;
        if (BindingGroupsContainScheme(binding.groups, scheme)) return true;
        if (!schemeDefinition.HasValue) return false;
        if (string.IsNullOrEmpty(binding.effectivePath)) return false;

        foreach (var requirement in schemeDefinition.Value.deviceRequirements)
        {
            if (string.IsNullOrEmpty(requirement.controlPath)) continue;
            var requiredLayout = requirement.controlPath.Trim('<', '>');
            if (string.IsNullOrEmpty(requiredLayout)) continue;
            if (binding.effectivePath.IndexOf(requiredLayout, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    private bool BindingGroupsContainScheme(string groups, string scheme)
    {
        if (string.IsNullOrEmpty(groups) || string.IsNullOrEmpty(scheme)) return false;
        var tokens = groups.Split(';');
        foreach (var token in tokens)
        {
            if (string.Equals(token.Trim(), scheme, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private void RebuildAliasLookup()
    {
        _aliasLookup = new Dictionary<string, BindingAlias>(StringComparer.OrdinalIgnoreCase);
        if (bindingAliases == null) return;
        foreach (var alias in bindingAliases)
        {
            if (alias == null || string.IsNullOrEmpty(alias.token)) continue;
            _aliasLookup[alias.token.Trim()] = alias;
        }
    }

    private BindingAlias GetAlias(string token)
    {
        if (string.IsNullOrEmpty(token) || _aliasLookup == null) return null;
        _aliasLookup.TryGetValue(token, out var alias);
        return alias;
    }

    private void BuildActionLookup(bool forceRebuild = false)
    {
        if (!forceRebuild && _actionLookup != null) return;
        var asset = playerInputHandler != null ? playerInputHandler.ControlsAsset : null;
        if (asset == null)
        {
            _actionLookup = null;
            return;
        }

        _actionLookup = new Dictionary<string, InputAction>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in asset.actionMaps)
        {
            foreach (var action in map.actions)
            {
                AddActionLookupEntry(action.name, action);
                AddActionLookupEntry($"{map.name}/{action.name}", action);
                AddActionLookupEntry($"{map.name}.{action.name}", action);
            }
        }
    }

    private void AddActionLookupEntry(string key, InputAction action)
    {
        if (string.IsNullOrEmpty(key) || action == null || _actionLookup == null) return;
        if (!_actionLookup.ContainsKey(key))
        {
            _actionLookup.Add(key, action);
        }
    }

    private InputAction FindActionByToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        BuildActionLookup();
        if (_actionLookup == null) return null;
        _actionLookup.TryGetValue(token, out var action);
        return action;
    }

    private void OnValidate()
    {
        RebuildAliasLookup();
    }

    private int FindCompositeRootIndex(UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputBinding> bindings, int partIndex)
    {
        for (int i = partIndex - 1; i >= 0; i--)
        {
            if (bindings[i].isComposite)
            {
                return i;
            }
            if (!bindings[i].isPartOfComposite)
            {
                break;
            }
        }
        return partIndex;
    }
}
