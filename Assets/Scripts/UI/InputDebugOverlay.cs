using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputDebugOverlay : MonoBehaviour
{
    private Text debugText;
    private Canvas overlayCanvas;

    void Awake()
    {
        overlayCanvas = new GameObject("InputDebugCanvas").AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999;
        DontDestroyOnLoad(overlayCanvas.gameObject);

        var textGO = new GameObject("InputDebugText");
        textGO.transform.SetParent(overlayCanvas.transform);
        debugText = textGO.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        debugText.fontSize = 18;
        debugText.color = Color.yellow;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.raycastTarget = false;
        var rect = debugText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(600, 120);
    }

    void Update()
    {
        string currentMap = "N/A";
        string scheme = "N/A";
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            currentMap = playerInput.currentActionMap != null ? playerInput.currentActionMap.name : "None";
            scheme = playerInput.currentControlScheme;
        }
        debugText.text = $"Input Map: {currentMap}\nControl Scheme: {scheme}\nActive Menu: {(IsMenuOpen() ? "Yes" : "No")}";
    }

    private bool IsMenuOpen()
    {
        var menu = FindObjectOfType<MenuController>();
        return menu != null && menu.IsMenuOpen;
    }
}
