using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GlobalLoadingOverlay : MonoBehaviour
{
    private static GlobalLoadingOverlay _instance;
    public static GlobalLoadingOverlay Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("GlobalLoadingOverlay");
                _instance = go.AddComponent<GlobalLoadingOverlay>();
                DontDestroyOnLoad(go);
                _instance.BuildUI();
            }
            return _instance;
        }
    }

    private Canvas _canvas;
    private Image _fadeImage;
    private CanvasGroup _loadingGroup;
    private Text _loadingLabel;
    private Image _barBg;
    private Image _barFill;

    public Image FadeImage => _fadeImage;
    public CanvasGroup LoadingGroup => _loadingGroup;
    public Text LoadingLabel => _loadingLabel;
    public Image LoadingBarBg => _barBg;
    public Image LoadingBarFill => _barFill;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        if (_canvas == null) BuildUI();
    }

    private void BuildUI()
    {
        if (_canvas != null) return;
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = short.MaxValue;
        gameObject.AddComponent<CanvasScaler>();

        var fadeGo = new GameObject("Fade");
        fadeGo.transform.SetParent(transform, false);
        _fadeImage = fadeGo.AddComponent<Image>();
        _fadeImage.color = new Color(0,0,0,0);
        var frt = _fadeImage.rectTransform; frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        _fadeImage.raycastTarget = false;

        var panel = new GameObject("LoadingPanel");
        panel.transform.SetParent(transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.15f);
        prt.anchorMax = new Vector2(0.5f, 0.15f);
        prt.sizeDelta = new Vector2(420, 80);
        prt.anchoredPosition = Vector2.zero;

        _loadingGroup = panel.AddComponent<CanvasGroup>();
        _loadingGroup.alpha = 0f;
        _loadingGroup.blocksRaycasts = false;
        _loadingGroup.interactable = false;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(panel.transform, false);
        _loadingLabel = labelGo.AddComponent<Text>();
        _loadingLabel.text = "Loading...";
        _loadingLabel.alignment = TextAnchor.MiddleCenter;
        _loadingLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _loadingLabel.fontSize = 24;
        var lrt = _loadingLabel.rectTransform; lrt.anchorMin = new Vector2(0.5f, 1f); lrt.anchorMax = new Vector2(0.5f, 1f); lrt.pivot = new Vector2(0.5f, 1f); lrt.sizeDelta = new Vector2(420, 30); lrt.anchoredPosition = new Vector2(0,0);

        var barBgGo = new GameObject("BarBG");
        barBgGo.transform.SetParent(panel.transform, false);
        _barBg = barBgGo.AddComponent<Image>();
        _barBg.color = new Color(1f,1f,1f,0.2f);
        var bgrt = _barBg.rectTransform; bgrt.anchorMin = new Vector2(0.5f, 0f); bgrt.anchorMax = new Vector2(0.5f, 0f); bgrt.pivot = new Vector2(0.5f, 0f); bgrt.sizeDelta = new Vector2(420, 20); bgrt.anchoredPosition = new Vector2(0, 0);

        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(barBgGo.transform, false);
        _barFill = fillGo.AddComponent<Image>();
        _barFill.color = new Color(1f,1f,1f,0.85f);
        var fr = _barFill.rectTransform; fr.anchorMin = new Vector2(0f,0f); fr.anchorMax = new Vector2(0f,1f); fr.pivot = new Vector2(0f,0.5f); fr.sizeDelta = new Vector2(0,0);
    }

    public void SetBlackImmediate(bool on)
    {
        if (_fadeImage == null) return;
        var c = _fadeImage.color; c.a = on ? 1f : 0f; _fadeImage.color = c;
        _fadeImage.raycastTarget = c.a > 0.02f;
    }

    public void SetLoadingVisible(bool visible, string text = null)
    {
        if (_loadingGroup == null) return;
        _loadingGroup.alpha = visible ? 1f : 0f;
        _loadingGroup.blocksRaycasts = visible;
        _loadingGroup.interactable = visible;
        if (!string.IsNullOrEmpty(text) && _loadingLabel != null) _loadingLabel.text = text;
    }

    public void SetProgress(float progress)
    {
        if (_barFill == null || _barBg == null) return;
        progress = Mathf.Clamp01(progress);
        var w = _barBg.rectTransform.rect.width;
        var rt = _barFill.rectTransform; rt.sizeDelta = new Vector2(w * progress, 0f);
    }

    public Coroutine LoadSceneAsync(MonoBehaviour host, string sceneName, float minShowSeconds = 2f, string text = "Loading...")
    {
        return host.StartCoroutine(DoLoadScene(sceneName, minShowSeconds, text));
    }

    private IEnumerator DoLoadScene(string sceneName, float minShowSeconds, string text)
    {
        BuildUI();
        SetBlackImmediate(true);
        SetLoadingVisible(true, text);
        SetProgress(0f);
        Canvas.ForceUpdateCanvases();
        yield return null;

        float start = Time.unscaledTime;
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            SetProgress(op.progress / 0.9f);
            yield return null;
        }
        SetProgress(1f);
        float elapsed = Time.unscaledTime - start;
        if (elapsed < minShowSeconds) yield return new WaitForSecondsRealtime(minShowSeconds - elapsed);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
        // fade out
        yield return FadeTo(0f, 0.2f);
        SetLoadingVisible(false);
    }

    public IEnumerator FadeTo(float target, float duration)
    {
        if (_fadeImage == null) yield break;
        float start = _fadeImage.color.a; float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
            var c = _fadeImage.color; c.a = a; _fadeImage.color = c;
            _fadeImage.raycastTarget = a > 0.02f;
            yield return null;
        }
        var cc = _fadeImage.color; cc.a = target; _fadeImage.color = cc;
        _fadeImage.raycastTarget = target > 0.02f;
    }
}
