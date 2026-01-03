using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Attach to any object (note on the ground, body, terminal, etc.) that the player can interact with
/// to read lore entries. When the player presses the interact key near this object, it will display
/// the assigned lore entries in sequence using LoreDialogueManager.
/// </summary>
public class LoreInteractable : MonoBehaviour
{
    [Header("Database Reference")]
    [Tooltip("The LoreDatabase to look up entries from. Can be left null if using direct entry references.")]
    [SerializeField] private LoreDatabase loreDatabase;

    [Header("Entries to Display")]
    [Tooltip("List of entry IDs to display from the database (in order).")]
    [SerializeField] private List<string> entryIds = new List<string>();

    [Tooltip("Alternatively, directly reference LoreEntry assets (used if entryIds is empty).")]
    [SerializeField] private List<LoreEntry> directEntries = new List<LoreEntry>();

    [Header("Interaction Settings")]
    [Tooltip("If true, the player must be within the trigger collider to interact.")]
    [SerializeField] private bool requireTriggerCollider = true;

    [Tooltip("If true, shows a UI prompt when the player is nearby.")]
    [SerializeField] private bool showInteractPrompt = true;

    [Tooltip("Custom prompt text (leave empty for default).")]
    [SerializeField] private string customPromptText = "";

    [Header("One-Time Read")]
    [Tooltip("If true, this lore can only be read once per game session.")]
    [SerializeField] private bool oneTimeOnly = false;

    [Tooltip("Unique ID for persistence (auto-generated from object path if empty).")]
    [SerializeField] private string persistenceId = "";

    [Header("Visual Feedback")]
    [Tooltip("Optional: Object to enable when player is nearby (e.g., a highlight or glow).")]
    [SerializeField] private GameObject highlightObject;

    [Tooltip("Optional: SpriteRenderer to change color when nearby.")]
    [SerializeField] private SpriteRenderer highlightSprite;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    private Color originalSpriteColor;

    // State
    private bool playerInRange;
    private bool hasBeenRead;
    private int currentEntryIndex;
    private bool isDisplayingSequence;
    private bool interactPressedThisFrame;

    // Player reference
    private GameObject playerObject;
    private Player_InputHandler playerInputHandler;
    private InputAction interactAction;

    private static HashSet<string> readLoreIds = new HashSet<string>();

    private void Start()
    {
        if (highlightSprite != null)
        {
            originalSpriteColor = highlightSprite.color;
        }

        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }

        // Generate persistence ID if empty
        if (string.IsNullOrEmpty(persistenceId))
        {
            persistenceId = $"{gameObject.scene.name}_{transform.GetSiblingIndex()}_{gameObject.name}";
        }

        // Check if already read
        if (oneTimeOnly && readLoreIds.Contains(persistenceId))
        {
            hasBeenRead = true;
        }
    }

    private void OnDisable()
    {
        // Clean up event subscription
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction = null;
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction = null;
        }
    }

    private void Update()
    {
        if (playerInRange && !isDisplayingSequence && !hasBeenRead)
        {
            // Check if interact was pressed this frame (set by event callback)
            if (interactPressedThisFrame)
            {
                interactPressedThisFrame = false;
                Debug.Log($"[LoreInteractable] Interact pressed, starting lore sequence on '{gameObject.name}'");
                StartLoreSequence();
            }
            // Fallback input check for legacy Input system
            else if (interactAction == null && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
            {
                Debug.Log($"[LoreInteractable] Fallback input detected, starting lore sequence on '{gameObject.name}'");
                StartLoreSequence();
            }
        }
    }

    private void LateUpdate()
    {
        // Reset the flag at end of frame
        interactPressedThisFrame = false;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (playerInRange && !isDisplayingSequence && !hasBeenRead)
        {
            interactPressedThisFrame = true;
            Debug.Log($"[LoreInteractable] OnInteractPerformed triggered on '{gameObject.name}'");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!requireTriggerCollider) return;
        if (!other.CompareTag("Player")) return;
        OnPlayerEnter(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!requireTriggerCollider) return;
        if (!other.CompareTag("Player")) return;
        OnPlayerExit();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!requireTriggerCollider) return;
        if (!other.CompareTag("Player")) return;
        OnPlayerEnter(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!requireTriggerCollider) return;
        if (!other.CompareTag("Player")) return;
        OnPlayerExit();
    }

    private void OnPlayerEnter(GameObject player)
    {
        if (hasBeenRead && oneTimeOnly) return;

        playerInRange = true;
        playerObject = player;
        playerInputHandler = player.GetComponent<Player_InputHandler>();

        // Subscribe to interact action for reliable input detection
        if (playerInputHandler != null && playerInputHandler.ControlsAsset != null)
        {
            var playerMap = playerInputHandler.ControlsAsset.FindActionMap("Player");
            interactAction = playerMap?.FindAction("Interact");
            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
                Debug.Log($"[LoreInteractable] Subscribed to Interact action on '{gameObject.name}'");
            }
        }

        // Show visual feedback
        if (highlightObject != null)
            highlightObject.SetActive(true);

        if (highlightSprite != null)
            highlightSprite.color = highlightColor;

        // Show interact prompt
        if (showInteractPrompt)
        {
            ShowPrompt();
        }
    }

    private void OnPlayerExit()
    {
        // Unsubscribe from interact action
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction = null;
            Debug.Log($"[LoreInteractable] Unsubscribed from Interact action on '{gameObject.name}'");
        }

        playerInRange = false;
        playerObject = null;
        playerInputHandler = null;

        // Hide visual feedback
        if (highlightObject != null)
            highlightObject.SetActive(false);

        if (highlightSprite != null)
            highlightSprite.color = originalSpriteColor;

        // Hide interact prompt
        if (showInteractPrompt)
        {
            HidePrompt();
        }
    }

    /// <summary>
    /// Call this to manually trigger the lore display (for non-trigger based interaction).
    /// </summary>
    public void TriggerInteraction()
    {
        if (!isDisplayingSequence && !hasBeenRead)
        {
            StartLoreSequence();
        }
    }

    private void StartLoreSequence()
    {
        Debug.Log($"[LoreInteractable] StartLoreSequence called on '{gameObject.name}'");
        
        var entries = GetEntriesToDisplay();
        Debug.Log($"[LoreInteractable] Found {entries.Count} entries to display");
        
        if (entries.Count == 0)
        {
            Debug.LogWarning($"[LoreInteractable] No entries to display on '{gameObject.name}'! Check that you have entries assigned in entryIds or directEntries.");
            return;
        }

        // Hide prompt while reading
        HidePrompt();

        isDisplayingSequence = true;
        currentEntryIndex = 0;
        
        Debug.Log($"[LoreInteractable] Calling DisplayCurrentEntry for entry index 0");
        DisplayCurrentEntry();
    }

    private void DisplayCurrentEntry()
    {
        var entries = GetEntriesToDisplay();
        if (currentEntryIndex >= entries.Count)
        {
            // Sequence complete
            OnSequenceComplete();
            return;
        }

        var entry = entries[currentEntryIndex];
        var dialogueManager = LoreDialogueManager.Instance ?? LoreDialogueManager.EnsureExists();

        dialogueManager.ShowEntry(entry, OnEntryComplete);
    }

    private void OnEntryComplete()
    {
        currentEntryIndex++;
        var entries = GetEntriesToDisplay();

        if (currentEntryIndex < entries.Count)
        {
            // Show next entry
            DisplayCurrentEntry();
        }
        else
        {
            OnSequenceComplete();
        }
    }

    private void OnSequenceComplete()
    {
        isDisplayingSequence = false;

        if (oneTimeOnly)
        {
            hasBeenRead = true;
            readLoreIds.Add(persistenceId);
        }

        // Re-show prompt if player still in range and can interact again
        if (playerInRange && !hasBeenRead && showInteractPrompt)
        {
            ShowPrompt();
        }
    }

    private List<LoreEntry> GetEntriesToDisplay()
    {
        var result = new List<LoreEntry>();

        // First try entry IDs from database
        if (entryIds.Count > 0 && loreDatabase != null)
        {
            foreach (var id in entryIds)
            {
                var entry = loreDatabase.GetEntry(id);
                if (entry != null)
                    result.Add(entry);
                else
                    Debug.LogWarning($"[LoreInteractable] Entry ID '{id}' not found in database!");
            }
        }

        // If no IDs or no database, use direct entries
        if (result.Count == 0 && directEntries.Count > 0)
        {
            foreach (var entry in directEntries)
            {
                if (entry != null)
                    result.Add(entry);
            }
        }

        return result;
    }

    #region Prompt UI

    private GameObject promptInstance;

    private void ShowPrompt()
    {
        // Simple implementation - you can replace this with your own prompt UI system
        if (promptInstance == null)
        {
            promptInstance = new GameObject("InteractPrompt");
            promptInstance.transform.SetParent(transform);
            promptInstance.transform.localPosition = Vector3.up * 1.5f;

            var canvas = promptInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var rectTransform = promptInstance.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);
            rectTransform.localScale = Vector3.one * 0.01f;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(promptInstance.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = string.IsNullOrEmpty(customPromptText) ? "[E] Read" : customPromptText;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontStyle = TMPro.FontStyles.Bold;

            // Add background
            var bg = promptInstance.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
        }

        promptInstance.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptInstance != null)
        {
            promptInstance.SetActive(false);
        }
    }

    #endregion

    #region Editor Helpers

    private void OnDrawGizmosSelected()
    {
        // Draw interaction radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        var collider2D = GetComponent<Collider2D>();
        var collider3D = GetComponent<Collider>();

        if (collider2D != null)
        {
            Gizmos.DrawWireCube(collider2D.bounds.center, collider2D.bounds.size);
        }
        else if (collider3D != null)
        {
            Gizmos.DrawWireCube(collider3D.bounds.center, collider3D.bounds.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    #endregion

    #region Static Methods

    /// <summary>
    /// Clears all read lore state (for new game).
    /// </summary>
    public static void ClearReadState()
    {
        readLoreIds.Clear();
    }

    /// <summary>
    /// Marks a lore ID as read (for loading saved games).
    /// </summary>
    public static void MarkAsRead(string persistenceId)
    {
        readLoreIds.Add(persistenceId);
    }

    /// <summary>
    /// Checks if a lore ID has been read.
    /// </summary>
    public static bool HasBeenRead(string persistenceId)
    {
        return readLoreIds.Contains(persistenceId);
    }

    /// <summary>
    /// Gets all read lore IDs (for saving).
    /// </summary>
    public static IEnumerable<string> GetAllReadIds()
    {
        return readLoreIds;
    }

    #endregion
}
