#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom editor for LoreInteractable that shows a dropdown of available entries from the database.
/// </summary>
[CustomEditor(typeof(LoreInteractable))]
public class LoreInteractableEditor : Editor
{
    private SerializedProperty loreDatabaseProp;
    private SerializedProperty entryIdsProp;
    private SerializedProperty directEntriesProp;
    private SerializedProperty requireTriggerColliderProp;
    private SerializedProperty showInteractPromptProp;
    private SerializedProperty customPromptTextProp;
    private SerializedProperty oneTimeOnlyProp;
    private SerializedProperty persistenceIdProp;
    private SerializedProperty highlightObjectProp;
    private SerializedProperty highlightSpriteProp;
    private SerializedProperty highlightColorProp;

    private void OnEnable()
    {
        loreDatabaseProp = serializedObject.FindProperty("loreDatabase");
        entryIdsProp = serializedObject.FindProperty("entryIds");
        directEntriesProp = serializedObject.FindProperty("directEntries");
        requireTriggerColliderProp = serializedObject.FindProperty("requireTriggerCollider");
        showInteractPromptProp = serializedObject.FindProperty("showInteractPrompt");
        customPromptTextProp = serializedObject.FindProperty("customPromptText");
        oneTimeOnlyProp = serializedObject.FindProperty("oneTimeOnly");
        persistenceIdProp = serializedObject.FindProperty("persistenceId");
        highlightObjectProp = serializedObject.FindProperty("highlightObject");
        highlightSpriteProp = serializedObject.FindProperty("highlightSprite");
        highlightColorProp = serializedObject.FindProperty("highlightColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Lore Interactable", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Database reference
        EditorGUILayout.PropertyField(loreDatabaseProp, new GUIContent("Lore Database"));

        var database = loreDatabaseProp.objectReferenceValue as LoreDatabase;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Entries to Display", EditorStyles.boldLabel);

        if (database != null && database.entries.Count > 0)
        {
            // Show entry IDs with dropdown selection
            EditorGUILayout.HelpBox("Select entries from the database below. They will be displayed in order.", MessageType.Info);

            for (int i = 0; i < entryIdsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var entryProp = entryIdsProp.GetArrayElementAtIndex(i);
                string currentId = entryProp.stringValue;

                // Create dropdown options
                var entryOptions = database.entries
                    .Where(e => e != null && !string.IsNullOrEmpty(e.entryId))
                    .Select(e => e.entryId)
                    .ToArray();

                int currentIndex = System.Array.IndexOf(entryOptions, currentId);
                if (currentIndex < 0) currentIndex = 0;

                EditorGUILayout.LabelField($"Entry {i + 1}", GUILayout.Width(60));
                int newIndex = EditorGUILayout.Popup(currentIndex, entryOptions);
                if (newIndex >= 0 && newIndex < entryOptions.Length)
                {
                    entryProp.stringValue = entryOptions[newIndex];
                }

                // Preview button
                if (GUILayout.Button("?", GUILayout.Width(25)))
                {
                    var entry = database.GetEntry(entryProp.stringValue);
                    if (entry != null)
                    {
                        EditorUtility.DisplayDialog($"Preview: {entry.title}", 
                            $"Title: {entry.title}\n\nBody:\n{entry.bodyText}", "OK");
                    }
                }

                // Remove button
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    entryIdsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Entry", GUILayout.Width(100)))
            {
                entryIdsProp.InsertArrayElementAtIndex(entryIdsProp.arraySize);
                if (database.entries.Count > 0 && database.entries[0] != null)
                {
                    entryIdsProp.GetArrayElementAtIndex(entryIdsProp.arraySize - 1).stringValue = 
                        database.entries[0].entryId;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a LoreDatabase to select entries from it, or use Direct Entries below.", MessageType.Info);
            EditorGUILayout.PropertyField(entryIdsProp, new GUIContent("Entry IDs (Manual)"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(directEntriesProp, new GUIContent("Direct Entries (Fallback)"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Interaction Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(requireTriggerColliderProp);
        EditorGUILayout.PropertyField(showInteractPromptProp);
        if (showInteractPromptProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(customPromptTextProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("One-Time Read", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(oneTimeOnlyProp);
        if (oneTimeOnlyProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(persistenceIdProp);
            if (string.IsNullOrEmpty(persistenceIdProp.stringValue))
            {
                EditorGUILayout.HelpBox("Persistence ID will be auto-generated at runtime if left empty.", MessageType.Info);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(highlightObjectProp);
        EditorGUILayout.PropertyField(highlightSpriteProp);
        if (highlightSpriteProp.objectReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(highlightColorProp);
            EditorGUI.indentLevel--;
        }

        // Validation
        EditorGUILayout.Space();
        var interactable = target as LoreInteractable;
        var collider2D = interactable.GetComponent<Collider2D>();
        var collider3D = interactable.GetComponent<Collider>();

        if (requireTriggerColliderProp.boolValue && collider2D == null && collider3D == null)
        {
            EditorGUILayout.HelpBox("No Collider found! Add a Collider2D or Collider with 'Is Trigger' enabled for interaction to work.", MessageType.Warning);
            if (GUILayout.Button("Add BoxCollider2D"))
            {
                var col = interactable.gameObject.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
            }
        }
        else if (requireTriggerColliderProp.boolValue)
        {
            bool isTrigger = (collider2D != null && collider2D.isTrigger) || 
                            (collider3D != null && collider3D.isTrigger);
            if (!isTrigger)
            {
                EditorGUILayout.HelpBox("Collider's 'Is Trigger' should be enabled for interaction detection.", MessageType.Warning);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
