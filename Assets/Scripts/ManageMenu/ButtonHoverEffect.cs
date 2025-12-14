using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Adds dynamic hover and click effects to UI buttons.
/// Attach this script to any Button GameObject for animated interactions.
/// </summary>
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Effects")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float animationSpeed = 10f;

    [Header("Color Effects")]
    [SerializeField] private bool useColorEffect = true;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 0.8f, 1f); // Ocean blue
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.75f, 0.95f, 1f); // Lighter blue
    [SerializeField] private Color pressedColor = new Color(0.15f, 0.5f, 0.7f, 1f); // Darker blue
    
    [Header("Text Effects")]
    [SerializeField] private bool animateText = true;
    [SerializeField] private Color textNormalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color textHoverColor = new Color(1f, 0.95f, 0.8f, 1f); // Warm white

    [Header("Movement Effect")]
    [SerializeField] private bool useSlideEffect = true;
    [SerializeField] private float slideOffset = 10f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Image buttonImage;
    private TextMeshProUGUI buttonText;
    private AudioSource audioSource;
    
    private float targetScale = 1f;
    private Color targetColor;
    private Color targetTextColor;
    private float targetXOffset = 0f;
    
    private bool isHovering = false;
    private bool isPressed = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null && (hoverSound != null || clickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Set initial colors
        if (buttonImage != null && useColorEffect)
        {
            buttonImage.color = normalColor;
            targetColor = normalColor;
        }
        
        if (buttonText != null && animateText)
        {
            buttonText.color = textNormalColor;
            targetTextColor = textNormalColor;
        }
    }

    private void Update()
    {
        // Smooth scale animation
        float currentTargetScale = isPressed ? clickScale : (isHovering ? hoverScale : 1f);
        Vector3 targetScaleVector = originalScale * currentTargetScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScaleVector, Time.unscaledDeltaTime * animationSpeed);

        // Smooth color animation
        if (buttonImage != null && useColorEffect)
        {
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.unscaledDeltaTime * animationSpeed);
        }
        
        if (buttonText != null && animateText)
        {
            buttonText.color = Color.Lerp(buttonText.color, targetTextColor, Time.unscaledDeltaTime * animationSpeed);
        }

        // Smooth slide animation
        if (useSlideEffect)
        {
            float targetX = originalPosition.x + targetXOffset;
            Vector3 targetPos = new Vector3(targetX, originalPosition.y, originalPosition.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        targetColor = hoverColor;
        targetTextColor = textHoverColor;
        targetXOffset = slideOffset;

        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        isPressed = false;
        targetColor = normalColor;
        targetTextColor = textNormalColor;
        targetXOffset = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetColor = pressedColor;

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, 0.7f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        targetColor = isHovering ? hoverColor : normalColor;
    }

    /// <summary>
    /// Call this to trigger a pulse animation (useful for drawing attention)
    /// </summary>
    public void TriggerPulse()
    {
        StartCoroutine(PulseAnimation());
    }

    private IEnumerator PulseAnimation()
    {
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            float scale = 1f + Mathf.Sin(elapsed / duration * Mathf.PI) * 0.1f;
            transform.localScale = originalScale * scale;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Reset button to original state
    /// </summary>
    public void ResetButton()
    {
        isHovering = false;
        isPressed = false;
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
        
        if (buttonImage != null) buttonImage.color = normalColor;
        if (buttonText != null) buttonText.color = textNormalColor;
    }
}
