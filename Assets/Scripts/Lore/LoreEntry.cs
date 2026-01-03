using UnityEngine;

/// <summary>
/// A single lore entry that can be displayed in dialogue boxes.
/// Create instances via Assets > Create > Lore > Lore Entry.
/// </summary>
[CreateAssetMenu(fileName = "NewLoreEntry", menuName = "Lore/Lore Entry", order = 1)]
public class LoreEntry : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Unique ID used to reference this entry in the database.")]
    public string entryId;

    [Header("Display")]
    [Tooltip("Title shown at the top of the dialogue box (optional).")]
    public string title;

    [Tooltip("The main body text. Use \\n for line breaks. Text will be paged if it exceeds the box.")]
    [TextArea(5, 20)]
    public string bodyText;

    [Header("Optional Visuals")]
    [Tooltip("Portrait or icon to display alongside the text (optional).")]
    public Sprite portrait;

    [Header("Optional Audio")]
    [Tooltip("Audio clip to play when this entry is shown (optional).")]
    public AudioClip voiceClip;

    [Tooltip("If true, the audio will play as SFX through AudioManager.")]
    public bool playAsSFX = true;

    [Header("Pagination")]
    [Tooltip("If body text is long, it will automatically paginate. This is the max characters per page (0 = auto based on UI).")]
    public int maxCharsPerPage = 0;

    /// <summary>
    /// Splits the body text into pages based on maxCharsPerPage or a provided limit.
    /// </summary>
    public string[] GetPages(int charsPerPage = 300)
    {
        int limit = maxCharsPerPage > 0 ? maxCharsPerPage : charsPerPage;
        if (string.IsNullOrEmpty(bodyText) || bodyText.Length <= limit)
        {
            return new string[] { bodyText ?? "" };
        }

        var pages = new System.Collections.Generic.List<string>();
        int start = 0;
        while (start < bodyText.Length)
        {
            int length = Mathf.Min(limit, bodyText.Length - start);
            // Try to break at a space or newline for cleaner pagination
            if (start + length < bodyText.Length)
            {
                int breakPoint = bodyText.LastIndexOf(' ', start + length, length);
                int newlineBreak = bodyText.LastIndexOf('\n', start + length, length);
                int bestBreak = Mathf.Max(breakPoint, newlineBreak);
                if (bestBreak > start)
                {
                    length = bestBreak - start;
                }
            }
            pages.Add(bodyText.Substring(start, length).Trim());
            start += length;
            // Skip the space/newline we broke on
            while (start < bodyText.Length && (bodyText[start] == ' ' || bodyText[start] == '\n'))
                start++;
        }
        return pages.ToArray();
    }
}
