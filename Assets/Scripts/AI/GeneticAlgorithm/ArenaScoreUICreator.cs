using UnityEngine;
using UnityEngine.UI;

public class ArenaScoreUICreator : MonoBehaviour
{
    void Awake()
    {
        // Create Canvas
        var canvasGO = new GameObject("ArenaScoreCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Score Text
        var textGO = new GameObject("ScoreText");
        textGO.transform.SetParent(canvasGO.transform);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 36;
        text.alignment = TextAnchor.UpperCenter;
        text.color = Color.white;
        text.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        text.rectTransform.pivot = new Vector2(0.5f, 1f);
        text.rectTransform.anchoredPosition = new Vector2(0, -40);
        text.rectTransform.sizeDelta = new Vector2(600, 60);

        // Create Best Genome Text
        var genomeGO = new GameObject("BestGenomeText");
        genomeGO.transform.SetParent(canvasGO.transform);
        var genomeText = genomeGO.AddComponent<Text>();
        genomeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        genomeText.fontSize = 28;
        genomeText.alignment = TextAnchor.UpperLeft;
        genomeText.color = Color.yellow;
        genomeText.rectTransform.anchorMin = new Vector2(0f, 1f);
        genomeText.rectTransform.anchorMax = new Vector2(0f, 1f);
        genomeText.rectTransform.pivot = new Vector2(0f, 1f);
        genomeText.rectTransform.anchoredPosition = new Vector2(20, -120);
        genomeText.rectTransform.sizeDelta = new Vector2(800, 220);

        // Add ArenaScoreUI
        var scoreUI = canvasGO.AddComponent<ArenaScoreUI>();
        scoreUI.scoreText = text;
        scoreUI.bestGenomeText = genomeText;
        canvasGO.AddComponent<ArenaScoreUIBootstrap>();
    }
}
