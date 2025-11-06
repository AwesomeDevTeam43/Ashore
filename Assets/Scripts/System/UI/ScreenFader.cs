using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Simple fullscreen fade-in/out overlay that's persistent across scenes.
public class ScreenFader : MonoBehaviour
{
  public static ScreenFader Instance { get; private set; }

  [SerializeField] private Canvas canvas;
  [SerializeField] private Image image;

  private Coroutine current;

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    if (canvas == null || image == null)
      BuildOverlay();

    SetAlpha(0f);
  }

  private void BuildOverlay()
  {
    var go = new GameObject("ScreenFaderCanvas");
    go.transform.SetParent(transform, false);
    canvas = go.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = short.MaxValue; // topmost
    go.AddComponent<CanvasGroup>();

    var imgGo = new GameObject("Fade");
    imgGo.transform.SetParent(go.transform, false);
    image = imgGo.AddComponent<Image>();
    image.color = Color.black;

    var rt = image.rectTransform;
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
  }

  private void SetAlpha(float a)
  {
    var c = image.color; c.a = Mathf.Clamp01(a); image.color = c;
  }

  public void FadeOutIn(float outDuration, float hold, float inDuration, System.Action middle)
  {
    if (current != null) StopCoroutine(current);
    current = StartCoroutine(FadeOutInRoutine(outDuration, hold, inDuration, middle));
  }

  public IEnumerator FadeOutInRoutine(float outDuration, float hold, float inDuration, System.Action middle)
  {
    yield return FadeTo(1f, outDuration);
    if (middle != null) middle();
    if (hold > 0f) yield return new WaitForSecondsRealtime(hold);
    yield return FadeTo(0f, inDuration);
    current = null;
  }

  public void FadeOut(float duration)
  {
    if (current != null) StopCoroutine(current);
    current = StartCoroutine(FadeTo(1f, duration));
  }

  public void FadeIn(float duration)
  {
    if (current != null) StopCoroutine(current);
    current = StartCoroutine(FadeTo(0f, duration));
  }

  private IEnumerator FadeTo(float target, float duration)
  {
    float start = image.color.a;
    float t = 0f;
    while (t < duration)
    {
      t += Time.unscaledDeltaTime;
      float a = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
      SetAlpha(a);
      yield return null;
    }
    SetAlpha(target);
  }
}
