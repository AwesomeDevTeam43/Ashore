using UnityEngine;
using UnityEngine.UI;

// Simple overlay that darkens the screen and leaves a rectangular hole to highlight a UI element
public class TutorialOverlay : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform root;
    private Image top, bottom, left, right;
    private Text message;

    public void BuildRuntimeCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1440);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // balanced
        gameObject.AddComponent<GraphicRaycaster>();
        root = canvas.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;

        top = CreatePanel("Top");
        bottom = CreatePanel("Bottom");
        left = CreatePanel("Left");
        right = CreatePanel("Right");

        var msgGO = new GameObject("Message", typeof(RectTransform), typeof(Text));
        msgGO.transform.SetParent(root, false);
        message = msgGO.GetComponent<Text>();
        var rt = msgGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.05f);
        rt.anchorMax = new Vector2(0.9f, 0.2f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        message.alignment = TextAnchor.MiddleCenter;
        message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        message.fontSize = 40;
        message.color = Color.white;
        message.text = "";

        SetDim(false);
        SetHighlight(null);
    }

    private Image CreatePanel(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // dimming disabled
        return img;
    }

    public void SetDim(bool dim)
    {
        // Dimming disabled globally
        float a = 0f;
        if (top != null) top.color = new Color(0,0,0,a);
        if (bottom != null) bottom.color = new Color(0,0,0,a);
        if (left != null) left.color = new Color(0,0,0,a);
        if (right != null) right.color = new Color(0,0,0,a);
    }

    // Allow manager to set dim alpha directly (e.g., reduce dim when inventory is open)
    public void SetDimLevel(float alpha)
    {
        // Dimming disabled globally
        float a = 0f;
        if (top != null) top.color = new Color(0,0,0,a);
        if (bottom != null) bottom.color = new Color(0,0,0,a);
        if (left != null) left.color = new Color(0,0,0,a);
        if (right != null) right.color = new Color(0,0,0,a);
    }

    public void SetMessage(string text)
    {
        if (message != null) message.text = text ?? "";
    }

    public void SetHighlight(RectTransform target)
    {
        // Adjust the four panels to frame the target rect, leaving it clear.
        if (root == null) return;
        Rect rect;
        if (target == null)
        {
            // Cover whole screen if no target
            rect = new Rect(root.rect.center, Vector2.zero);
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(null, target.position), null, out Vector2 localCenter);
            Vector2 size = target.rect.size;
            rect = new Rect(localCenter - size * 0.5f, size);
        }

        // Panels are positioned in local space of root rect transform
        float xMin = rect.xMin; float xMax = rect.xMax;
        float yMin = rect.yMin; float yMax = rect.yMax;

        // Top panel: covers area above highlight
        SetPanelRect(top.rectTransform, new Vector2(0, yMax), new Vector2(root.rect.width, root.rect.height - yMax));
        // Bottom panel: below highlight
        SetPanelRect(bottom.rectTransform, new Vector2(0, 0), new Vector2(root.rect.width, yMin));
        // Left panel: left of highlight
        SetPanelRect(left.rectTransform, new Vector2(0, yMin), new Vector2(xMin, yMax - yMin));
        // Right panel: right of highlight
        SetPanelRect(right.rectTransform, new Vector2(xMax, yMin), new Vector2(root.rect.width - xMax, yMax - yMin));
    }

    private void SetPanelRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
