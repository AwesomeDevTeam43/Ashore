using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Adds a simple visual highlight (Outline) when this selectable is focused via keyboard/controller
// or hovered/clicked with the mouse. Works with EventSystem selection.
public class SelectionHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color outlineColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private float outlineDistance = 2f;
    [Header("Optional Background Tint")]
    [Tooltip("If true (or when an empty transparent Image is detected), this will tint the background slightly for clearer highlighting.")]
    [SerializeField] private bool tintBackgroundIfEmpty = true;
    [SerializeField] private Color idleBackground = new Color(0f, 0f, 0f, 0.18f);
    [SerializeField] private Color activeBackground = new Color(0.18f, 0.15f, 0.06f, 0.35f);

    private Outline outline;
    private Image bgImage;
    private bool manageBackground = false;
    private bool isSelected;
    private bool isHovered;
    private bool pointerMode; // true = mouse/pointer mode, false = controller/keyboard mode

    private void Awake()
    {
        EnsureOutline();
        // Determine if we should manage background tinting
        bgImage = GetComponent<Image>();
        if (tintBackgroundIfEmpty && bgImage != null)
        {
            // If there's no visible color (alpha near zero), we'll manage tinting
            manageBackground = bgImage.color.a <= 0.01f;
            if (manageBackground)
            {
                bgImage.color = idleBackground;
            }
        }
        SetActive(false);
    }

    private void OnEnable()
    {
        UIInputMode.OnChanged += OnModeChanged;
        pointerMode = UIInputMode.Current == UIInputMode.Mode.Pointer;
        UpdateActive();
    }

    private void OnDisable()
    {
        UIInputMode.OnChanged -= OnModeChanged;
    }

    private void EnsureOutline()
    {
        // Ensure there is a Graphic to render the outline against; add a transparent Image if none
        var graphic = GetComponent<Graphic>();
        if (graphic == null)
        {
            var img = gameObject.AddComponent<Image>();
            img.color = new Color(1f,1f,1f,0f);
            img.raycastTarget = true;
            graphic = img;
        }

        outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(outlineDistance, outlineDistance);
    }

    public void SetColors(Color unusedBackground, Color highlight)
    {
        outlineColor = highlight;
        if (outline != null) outline.effectColor = outlineColor;
    }

    private void SetActive(bool on)
    {
        if (outline != null)
        {
            outline.enabled = on;
        }
        if (manageBackground && bgImage != null)
        {
            bgImage.color = on ? activeBackground : idleBackground;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        UpdateActive();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        UpdateActive();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UIInputMode.Set(UIInputMode.Mode.Pointer);
        UpdateActive();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateActive();
    }

    private void OnModeChanged(UIInputMode.Mode m)
    {
        pointerMode = (m == UIInputMode.Mode.Pointer);
        UpdateActive();
    }

    private void UpdateActive()
    {
        // In pointer mode, only hover drives highlight; in controller mode, only selection drives highlight
        bool on = pointerMode ? isHovered : isSelected;
        SetActive(on);
    }
}
