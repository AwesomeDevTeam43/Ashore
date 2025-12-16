using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Handles audio settings UI and persistence.
/// </summary>
public class AudioSettingsController
{
    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;

    public Slider MasterVolumeSlider => masterVolumeSlider;
    public Slider MusicVolumeSlider => musicVolumeSlider;
    public Slider SFXVolumeSlider => sfxVolumeSlider;

    public GameObject CreateAudioTabContent(Transform parent)
    {
        GameObject content = new GameObject("AudioContent");
        content.transform.SetParent(parent, false);

        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // Ensure AudioManager exists (try to find, else instantiate from Resources if available)
        if (AudioManager.Instance == null)
        {
            var found = GameObject.FindObjectOfType<AudioManager>();
            if (found == null)
            {
                var prefab = Resources.Load<GameObject>("AudioManager");
                if (prefab != null)
                {
                    GameObject.Instantiate(prefab);
                }
                else
                {
                    Debug.LogError("AudioManager prefab not found in Resources! Please add one to your first scene or Resources folder.");
                }
            }
        }

        // Master Volume
        masterVolumeSlider = CreateVolumeSlider(content.transform, "Master Volume", 0.75f, (value) =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMasterVolume(value);
            PlayerPrefs.SetFloat("MasterVolume", value);
        });
        float masterValue = PlayerPrefs.GetFloat("MasterVolume", 1f);
        masterVolumeSlider.value = masterValue;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(masterValue);

        // Music Volume
        musicVolumeSlider = CreateVolumeSlider(content.transform, "Music Volume", 0.50f, (value) =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
            PlayerPrefs.SetFloat("MusicVolume", value);
        });
        float musicValue = PlayerPrefs.GetFloat("MusicVolume", 1f);
        musicVolumeSlider.value = musicValue;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(musicValue);



        // SFX Volume
        sfxVolumeSlider = CreateVolumeSlider(content.transform, "SFX Volume", 0.25f, (value) =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(value);
            PlayerPrefs.SetFloat("SFXVolume", value);
        });
        float sfxValue = PlayerPrefs.GetFloat("SFXVolume", 1f);
        sfxVolumeSlider.value = sfxValue;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(sfxValue);

        // Setup slider navigation
        SetupSliderNavigation(masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider);

        return content;
    }

    private Slider CreateVolumeSlider(Transform parent, string label, float yPosition,
        UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        int fontSize = PauseMenuUIFactory.ButtonFontSize;

        // Container
        GameObject container = new GameObject("Slider_" + label);
        container.transform.SetParent(parent, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, yPosition - 0.08f);
        containerRect.anchorMax = new Vector2(0.9f, yPosition + 0.08f);
        containerRect.sizeDelta = Vector2.zero;

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(container.transform, false);

        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0.35f, 1f);
        labelRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = fontSize - 12;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.Left;

        // Slider
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(container.transform, false);

        RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.4f, 0.3f);
        sliderRect.anchorMax = new Vector2(0.9f, 0.7f);
        sliderRect.sizeDelta = Vector2.zero;

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.sizeDelta = new Vector2(-20f, 0f);
        fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = PauseMenuUIFactory.ButtonColor;

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 0f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;

        // Slider component
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.onValueChanged.AddListener(onValueChanged);

        // Value display
        GameObject valueGO = new GameObject("Value");
        valueGO.transform.SetParent(container.transform, false);
        RectTransform valueRect = valueGO.AddComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.92f, 0.3f);
        valueRect.anchorMax = new Vector2(1f, 0.7f);
        valueRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI valueTmp = valueGO.AddComponent<TextMeshProUGUI>();
        valueTmp.text = "100%";
        valueTmp.fontSize = fontSize - 16;
        valueTmp.color = Color.white;
        valueTmp.alignment = TextAlignmentOptions.Center;

        // Update value text when slider changes
        slider.onValueChanged.AddListener((value) =>
        {
            valueTmp.text = Mathf.RoundToInt(value * 100) + "%";
        });

        // Set automatic navigation
        Navigation nav = slider.navigation;
        nav.mode = Navigation.Mode.Automatic;
        slider.navigation = nav;

        // Add UINavTarget and SelectionHighlight
        sliderGO.AddComponent<UINavTarget>();
        sliderGO.AddComponent<SelectionHighlight>();

        return slider;
    }

    private void SetupSliderNavigation(params Slider[] sliders)
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] == null) continue;

            Navigation nav = sliders[i].navigation;
            nav.mode = Navigation.Mode.Automatic;
            sliders[i].navigation = nav;
        }
    }

    public void SetupOptionsNavigation(Button backButton)
    {
        if (masterVolumeSlider != null)
        {
            Navigation nav = masterVolumeSlider.navigation;
            nav.mode = Navigation.Mode.Automatic;
            masterVolumeSlider.navigation = nav;
        }
        if (musicVolumeSlider != null)
        {
            Navigation nav = musicVolumeSlider.navigation;
            nav.mode = Navigation.Mode.Automatic;
            musicVolumeSlider.navigation = nav;
        }
        if (sfxVolumeSlider != null)
        {
            Navigation nav = sfxVolumeSlider.navigation;
            nav.mode = Navigation.Mode.Automatic;
            sfxVolumeSlider.navigation = nav;
        }
        if (backButton != null)
        {
            Navigation nav = backButton.navigation;
            nav.mode = Navigation.Mode.Automatic;
            backButton.navigation = nav;
        }
    }
}
