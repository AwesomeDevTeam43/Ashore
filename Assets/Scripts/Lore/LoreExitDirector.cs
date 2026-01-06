using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LoreExitDirector : MonoBehaviour
{
    [Tooltip("LoreInteractable that will trigger the fade/scene change when its sequence completes.")]
    [SerializeField] private LoreInteractable loreInteractable;

    [Tooltip("Scene that will load after the fade completes.")]
    [SerializeField] private string targetScene = "MainMenu";
    [Tooltip("How long the fade takes before loading the new scene.")]
    [SerializeField] private float fadeDuration = 2f;
    [Tooltip("Color used for the fade overlay.")]
    [SerializeField] private Color fadeColor = Color.black;

    private CanvasGroup fadeCanvasGroup;
    private Image fadeImage;
    private bool hasTriggered = false;

    private void Reset()
    {
        loreInteractable = GetComponent<LoreInteractable>();
    }

    private void Awake()
    {
        if (loreInteractable == null)
            loreInteractable = GetComponent<LoreInteractable>();
    }

    private void OnEnable()
    {
        if (loreInteractable != null)
            loreInteractable.SequenceCompleted += HandleLoreComplete;
    }

    private void OnDisable()
    {
        if (loreInteractable != null)
            loreInteractable.SequenceCompleted -= HandleLoreComplete;
    }

    private void HandleLoreComplete()
    {
        if (hasTriggered) return;
        hasTriggered = true;
        StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine()
    {
        EnsureFadeUI();
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 1f;

        SceneManager.LoadScene(targetScene);
    }

    private void EnsureFadeUI()
    {
        if (fadeCanvasGroup != null && fadeImage != null) return;

        var canvasGO = new GameObject("LoreFadeCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        fadeCanvasGroup = canvasGO.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = true;

        var imageGO = new GameObject("FadeImage", typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);
        fadeImage = imageGO.GetComponent<Image>();
        fadeImage.color = fadeColor;
        var rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
