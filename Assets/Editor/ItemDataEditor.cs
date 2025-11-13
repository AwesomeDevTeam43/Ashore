using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    SerializedProperty isCraftableProp;
    SerializedProperty craftIngredientsProp;
    SerializedProperty craftZoneProp;
    SerializedProperty craftResultProp;

    void OnEnable()
    {
        isCraftableProp = serializedObject.FindProperty("isCraftable");
        craftIngredientsProp = serializedObject.FindProperty("craftIngredients");
        craftZoneProp = serializedObject.FindProperty("craftZone");
        craftResultProp = serializedObject.FindProperty("craftResult");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw all properties except the conditionally-shown ones,
        // we'll draw them manually depending on isCraftable.
        DrawPropertiesExcluding(serializedObject, "craftIngredients", "craftZone", "craftResult");

        // Show crafting ingredients when isCraftable is true
        if (isCraftableProp != null && isCraftableProp.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Crafting", EditorStyles.boldLabel);
            if (craftIngredientsProp != null)
                EditorGUILayout.PropertyField(craftIngredientsProp, true);
            else
                EditorGUILayout.HelpBox("craftIngredients property not found on ItemData.", MessageType.Warning);

            if (craftResultProp != null)
                EditorGUILayout.PropertyField(craftResultProp, new GUIContent("Craft Result (optional)"));
            else
                EditorGUILayout.HelpBox("craftResult property not found on ItemData.", MessageType.Info);
        }

        // Optionally show craftZone (unchanged behavior)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawn / Craft Zone (optional)", EditorStyles.boldLabel);
        if (craftZoneProp != null)
            EditorGUILayout.PropertyField(craftZoneProp, true);
        else
            EditorGUILayout.HelpBox("craftZone property not found on ItemData.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}