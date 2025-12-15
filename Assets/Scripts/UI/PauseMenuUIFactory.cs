using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Factory class for creating UI elements used in pause menu.
/// </summary>
public static class PauseMenuUIFactory
{
    public static Color ButtonColor = new Color(0.2f, 0.6f, 0.85f, 1f);
    public static Color ButtonHoverColor = new Color(0.3f, 0.75f, 0.95f, 1f);
    public static int ButtonFontSize = 42;
    public static int TitleFontSize = 72;

    public static void CreateTextElement(Transform parent, string text, int fontSize, 
        Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject textGO = new GameObject("Text_" + text);
        textGO.transform.SetParent(parent, false);
        
        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        
        // Add gradient effect for title
        if (fontSize >= 60)
        {
            tmp.enableVertexGradient = true;
            tmp.colorGradient = new VertexGradient(
                new Color(0.6f, 0.85f, 1f),
                new Color(0.6f, 0.85f, 1f),
                new Color(1f, 0.95f, 0.8f),
                new Color(1f, 0.95f, 0.8f)
            );
        }
    }

    public static Button CreateButton(Transform parent, string text, 
        Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject("Button_" + text);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        
        Image image = buttonGO.AddComponent<Image>();
        image.color = ButtonColor;
        
        Button button = buttonGO.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = new Color(ButtonColor.r * 0.8f, ButtonColor.g * 0.8f, ButtonColor.b * 0.8f);
        colors.selectedColor = ButtonHoverColor;
        colors.fadeDuration = 0.15f;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        
        // Set automatic navigation
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;
        
        // Add UINavTarget and SelectionHighlight for inventory-style navigation
        buttonGO.AddComponent<UINavTarget>();
        buttonGO.AddComponent<SelectionHighlight>();
        
        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = ButtonFontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        return button;
    }

    public static Button CreateTabButton(Transform parent, string text, 
        Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject("Tab_" + text);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        
        Image image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        
        Button button = buttonGO.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        colors.highlightedColor = ButtonHoverColor;
        colors.selectedColor = ButtonHoverColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        
        // Set automatic navigation
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;
        
        // Add UINavTarget and SelectionHighlight
        buttonGO.AddComponent<UINavTarget>();
        buttonGO.AddComponent<SelectionHighlight>();
        
        // Tab text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = ButtonFontSize - 8;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        return button;
    }

    public static Button CreateSmallButton(Transform parent, string text, 
        Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick, 
        Color? normalColor = null, Color? hoverColor = null)
    {
        Color normal = normalColor ?? new Color(0.6f, 0.3f, 0.3f, 1f);
        Color hover = hoverColor ?? new Color(0.8f, 0.4f, 0.4f, 1f);
        
        GameObject buttonGO = new GameObject("Button_" + text);
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        
        Image image = buttonGO.AddComponent<Image>();
        image.color = normal;
        
        Button button = buttonGO.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = hover;
        colors.pressedColor = new Color(normal.r * 0.8f, normal.g * 0.8f, normal.b * 0.8f);
        colors.selectedColor = hover;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        
        // Set automatic navigation
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Automatic;
        button.navigation = nav;
        
        // Add UINavTarget and SelectionHighlight
        buttonGO.AddComponent<UINavTarget>();
        buttonGO.AddComponent<SelectionHighlight>();
        
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = ButtonFontSize - 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        return button;
    }

    public static GameObject CreatePanel(Transform parent, string name, Color backgroundColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image bgImage = panel.AddComponent<Image>();
        bgImage.color = backgroundColor;
        
        // Add UINavScope for inventory-style navigation
        panel.AddComponent<UINavScope>();
        
        return panel;
    }

    public static GameObject CreateContainer(Transform parent, string name, 
        Vector2 anchorMin, Vector2 anchorMax, Color? backgroundColor = null)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);
        
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        
        if (backgroundColor.HasValue)
        {
            Image bg = container.AddComponent<Image>();
            bg.color = backgroundColor.Value;
        }
        
        return container;
    }
}
