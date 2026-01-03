using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Singleton manager that displays lore entries in an RPGMaker-style dialogue box.
/// Handles pagination, player input locking, and optional audio playback.
/// </summary>
public class LoreDialogueManager : MonoBehaviour
{
    public static LoreDialogueManager Instance { get; private set; }

    [Header("UI References (Auto-created if null)")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject continuePrompt;

    [Header("Appearance Settings")]
    [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color titleColor = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private int bodyFontSize = 28;
    [SerializeField] private int titleFontSize = 36;

    [Header("Pagination")]
    [SerializeField] private int defaultCharsPerPage = 300;

    [Header("Typewriter Effect")]
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("Input")]
    [SerializeField] private InputActionReference confirmAction;
    [SerializeField] private InputActionReference cancelAction;

    // State
    private LoreEntry currentEntry;
    private string[] currentPages;
    private int currentPageIndex;
    private bool isDisplaying;
    private bool isTyping;
    private Coroutine typewriterCoroutine;
    private Action onDialogueComplete;

    public bool IsDisplaying => isDisplaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[LoreDialogueManager] Awake - creating UI...");
        EnsureUIExists();
        HideDialogue();
        Debug.Log("[LoreDialogueManager] Awake complete, UI created and hidden");
    }

    private void OnEnable()
    {
        if (confirmAction != null && confirmAction.action != null)
        {
            confirmAction.action.performed += OnConfirmPressed;
            confirmAction.action.Enable();
        }
        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.performed += OnCancelPressed;
            cancelAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (confirmAction != null && confirmAction.action != null)
        {
            confirmAction.action.performed -= OnConfirmPressed;
        }
        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.performed -= OnCancelPressed;
        }
    }

    private void Update()
    {
        // Fallback input detection if no InputActionReference is set
        if (isDisplaying && confirmAction == null)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || 
                Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Submit"))
            {
                AdvanceDialogue();
            }
        }
        if (isDisplaying && cancelAction == null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseDialogue();
            }
        }
    }

    #region Public API

    /// <summary>
    /// Shows a lore entry by directly passing the entry object.
    /// </summary>
    public void ShowEntry(LoreEntry entry, Action onComplete = null)
    {
        if (entry == null)
        {
            Debug.LogWarning("[LoreDialogueManager] Attempted to show null entry!");
            return;
        }

        Debug.Log($"[LoreDialogueManager] ShowEntry called: '{entry.title}' with {entry.bodyText?.Length ?? 0} chars");

        currentEntry = entry;
        onDialogueComplete = onComplete;
        currentPages = entry.GetPages(defaultCharsPerPage);
        currentPageIndex = 0;

        Debug.Log($"[LoreDialogueManager] Entry has {currentPages.Length} page(s)");

        // Lock player input
        LockPlayer();

        // Setup UI
        if (titleText != null)
        {
            titleText.text = entry.title ?? "";
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(entry.title));
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = entry.portrait;
            portraitImage.gameObject.SetActive(entry.portrait != null);
        }

        // Play audio if present
        if (entry.voiceClip != null && AudioManager.Instance != null)
        {
            if (entry.playAsSFX)
                AudioManager.Instance.PlaySFX(entry.voiceClip);
            else
                AudioManager.Instance.PlayMusic(entry.voiceClip);
        }

        ShowDialogue();
        DisplayCurrentPage();
        
        Debug.Log($"[LoreDialogueManager] Dialogue shown, isDisplaying={isDisplaying}");
    }

    /// <summary>
    /// Shows a lore entry by ID, looking it up in the provided database.
    /// </summary>
    public void ShowEntryById(string entryId, LoreDatabase database, Action onComplete = null)
    {
        if (database == null)
        {
            Debug.LogError("[LoreDialogueManager] No LoreDatabase provided!");
            return;
        }

        var entry = database.GetEntry(entryId);
        if (entry == null)
        {
            Debug.LogWarning($"[LoreDialogueManager] Entry '{entryId}' not found in database!");
            return;
        }

        ShowEntry(entry, onComplete);
    }

    /// <summary>
    /// Shows raw text without a LoreEntry (for dynamic dialogue).
    /// </summary>
    public void ShowText(string title, string body, Sprite portrait = null, Action onComplete = null)
    {
        // Create a temporary runtime entry
        var tempEntry = ScriptableObject.CreateInstance<LoreEntry>();
        tempEntry.title = title;
        tempEntry.bodyText = body;
        tempEntry.portrait = portrait;
        ShowEntry(tempEntry, () =>
        {
            Destroy(tempEntry);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Advances to the next page or closes if on the last page.
    /// </summary>
    public void AdvanceDialogue()
    {
        if (!isDisplaying) return;

        // If currently typing, complete the text instantly
        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        // Advance to next page
        currentPageIndex++;
        if (currentPageIndex < currentPages.Length)
        {
            DisplayCurrentPage();
        }
        else
        {
            CloseDialogue();
        }
    }

    /// <summary>
    /// Immediately closes the dialogue.
    /// </summary>
    public void CloseDialogue()
    {
        if (!isDisplaying) return;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;
        isDisplaying = false;
        HideDialogue();
        UnlockPlayer();

        var callback = onDialogueComplete;
        onDialogueComplete = null;
        currentEntry = null;
        callback?.Invoke();
    }

    #endregion

    #region Display Logic

    private void DisplayCurrentPage()
    {
        if (bodyText == null || currentPages == null || currentPageIndex >= currentPages.Length)
            return;

        string pageContent = currentPages[currentPageIndex];

        // Update page indicator
        if (pageIndicatorText != null)
        {
            if (currentPages.Length > 1)
            {
                pageIndicatorText.text = $"{currentPageIndex + 1}/{currentPages.Length}";
                pageIndicatorText.gameObject.SetActive(true);
            }
            else
            {
                pageIndicatorText.gameObject.SetActive(false);
            }
        }

        // Show continue prompt
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false); // Will be shown after typing completes
        }

        // Display text
        if (useTypewriterEffect)
        {
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterEffect(pageContent));
        }
        else
        {
            bodyText.text = pageContent;
            OnTypingComplete();
        }
    }

    private IEnumerator TypewriterEffect(string text)
    {
        isTyping = true;
        bodyText.text = "";

        foreach (char c in text)
        {
            bodyText.text += c;
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        OnTypingComplete();
    }

    private void CompleteTyping()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (currentPages != null && currentPageIndex < currentPages.Length)
        {
            bodyText.text = currentPages[currentPageIndex];
        }

        OnTypingComplete();
    }

    private void OnTypingComplete()
    {
        isTyping = false;
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
        }
    }

    private void ShowDialogue()
    {
        isDisplaying = true;
        Debug.Log($"[LoreDialogueManager] ShowDialogue: canvas={dialogueCanvas != null}, panel={dialoguePanel != null}");
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(true);
            Debug.Log($"[LoreDialogueManager] Canvas activated, sortingOrder={dialogueCanvas.sortingOrder}");
        }
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            Debug.Log($"[LoreDialogueManager] Panel activated");
        }
    }

    private void HideDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);
    }

    #endregion

    #region Input Handling

    private void OnConfirmPressed(InputAction.CallbackContext context)
    {
        if (isDisplaying)
        {
            AdvanceDialogue();
        }
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        if (isDisplaying)
        {
            CloseDialogue();
        }
    }

    #endregion

    #region Player Lock

    private void LockPlayer()
    {
        if (PlayerStateLockController.Instance != null)
        {
            PlayerStateLockController.Instance.RequestLock("LoreDialogue");
        }
        else
        {
            // Fallback: try to find player and disable input directly
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var inputHandler = player.GetComponent<Player_InputHandler>();
                if (inputHandler != null)
                    inputHandler.DisablePlayerActions();
            }
        }
    }

    private void UnlockPlayer()
    {
        if (PlayerStateLockController.Instance != null)
        {
            PlayerStateLockController.Instance.ReleaseLock("LoreDialogue");
        }
        else
        {
            // Fallback
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var inputHandler = player.GetComponent<Player_InputHandler>();
                if (inputHandler != null)
                    inputHandler.EnablePlayerActions();
            }
        }
    }

    #endregion

    #region UI Creation

    private void EnsureUIExists()
    {
        if (dialogueCanvas != null && dialoguePanel != null && bodyText != null)
            return;

        // Create canvas
        if (dialogueCanvas == null)
        {
            GameObject canvasGO = new GameObject("LoreDialogueCanvas");
            canvasGO.transform.SetParent(transform);
            dialogueCanvas = canvasGO.AddComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 999;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create panel (bottom of screen, RPGMaker style)
        if (dialoguePanel == null)
        {
            dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(dialogueCanvas.transform, false);

            RectTransform panelRect = dialoguePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.02f);
            panelRect.anchorMax = new Vector2(0.95f, 0.32f);
            panelRect.sizeDelta = Vector2.zero;

            Image panelImage = dialoguePanel.AddComponent<Image>();
            panelImage.color = panelColor;

            // Add slight border effect
            Outline outline = dialoguePanel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
            outline.effectDistance = new Vector2(2, 2);
        }

        // Create portrait area (left side)
        if (portraitImage == null)
        {
            GameObject portraitGO = new GameObject("Portrait");
            portraitGO.transform.SetParent(dialoguePanel.transform, false);

            RectTransform portraitRect = portraitGO.AddComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.02f, 0.1f);
            portraitRect.anchorMax = new Vector2(0.15f, 0.9f);
            portraitRect.sizeDelta = Vector2.zero;

            portraitImage = portraitGO.AddComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitGO.SetActive(false);
        }

        // Create title text
        if (titleText == null)
        {
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(dialoguePanel.transform, false);

            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.17f, 0.75f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.sizeDelta = Vector2.zero;

            titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = titleFontSize;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = titleColor;
            titleText.alignment = TextAlignmentOptions.TopLeft;
        }

        // Create body text
        if (bodyText == null)
        {
            GameObject bodyGO = new GameObject("BodyText");
            bodyGO.transform.SetParent(dialoguePanel.transform, false);

            RectTransform bodyRect = bodyGO.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.17f, 0.1f);
            bodyRect.anchorMax = new Vector2(0.95f, 0.72f);
            bodyRect.sizeDelta = Vector2.zero;

            bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
            bodyText.fontSize = bodyFontSize;
            bodyText.color = textColor;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
        }

        // Create page indicator
        if (pageIndicatorText == null)
        {
            GameObject pageGO = new GameObject("PageIndicator");
            pageGO.transform.SetParent(dialoguePanel.transform, false);

            RectTransform pageRect = pageGO.AddComponent<RectTransform>();
            pageRect.anchorMin = new Vector2(0.85f, 0.02f);
            pageRect.anchorMax = new Vector2(0.98f, 0.12f);
            pageRect.sizeDelta = Vector2.zero;

            pageIndicatorText = pageGO.AddComponent<TextMeshProUGUI>();
            pageIndicatorText.fontSize = 20;
            pageIndicatorText.color = new Color(0.7f, 0.7f, 0.7f);
            pageIndicatorText.alignment = TextAlignmentOptions.BottomRight;
        }

        // Create continue prompt
        if (continuePrompt == null)
        {
            GameObject promptGO = new GameObject("ContinuePrompt");
            promptGO.transform.SetParent(dialoguePanel.transform, false);

            RectTransform promptRect = promptGO.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.02f, 0.02f);
            promptRect.anchorMax = new Vector2(0.5f, 0.12f);
            promptRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI promptText = promptGO.AddComponent<TextMeshProUGUI>();
            promptText.text = "Press [E] or [Space] to continue...";
            promptText.fontSize = 18;
            promptText.color = new Color(0.6f, 0.8f, 1f);
            promptText.fontStyle = FontStyles.Italic;
            promptText.alignment = TextAlignmentOptions.BottomLeft;

            continuePrompt = promptGO;
        }
    }

    #endregion

    #region Static Helper

    /// <summary>
    /// Ensures a LoreDialogueManager exists in the scene.
    /// </summary>
    public static LoreDialogueManager EnsureExists()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("LoreDialogueManager");
            Instance = go.AddComponent<LoreDialogueManager>();
        }
        return Instance;
    }

    #endregion
}
