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
        DrawPropertiesExcluding(serializedObject, "craftIngredients", "craftZone");

        // Show crafting ingredients when isCraftable is true
        if (isCraftableProp != null && isCraftableProp.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Crafting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(craftIngredientsProp, true);
            EditorGUILayout.PropertyField(craftResultProp, new GUIContent("Craft Result (optional)"));
        }

        // Optionally show craftZone (unchanged behavior)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawn / Craft Zone (optional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(craftZoneProp, true);

        serializedObject.ApplyModifiedProperties();
    }
}