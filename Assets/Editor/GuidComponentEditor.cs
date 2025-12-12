using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuidComponent))]
public class GuidComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields
        base.OnInspectorGUI();

        // Get the target component
        GuidComponent guidComponent = (GuidComponent)target;

        EditorGUILayout.Space(10);
        
        // Show current GUID status
        if (guidComponent.HasValidGuid)
        {
            EditorGUILayout.HelpBox("✓ GUID is set and will persist forever.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠ No GUID set! One will be auto-generated.", MessageType.Warning);
        }
        
        EditorGUILayout.Space(5);
        
        // Add a button to force regenerate (with warning)
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("⚠️ Force Regenerate GUID (Breaks Saves!)"))
        {
            if (EditorUtility.DisplayDialog(
                "Regenerate GUID?",
                "This will generate a NEW GUID for this object.\n\n" +
                "⚠️ WARNING: Any existing save data referencing this object will be BROKEN!\n\n" +
                "Only do this for duplicated objects that need unique IDs.",
                "Regenerate",
                "Cancel"))
            {
                guidComponent.ForceRegenerateGuid();
                EditorUtility.SetDirty(guidComponent);
            }
        }
        GUI.backgroundColor = Color.white;
    }
}