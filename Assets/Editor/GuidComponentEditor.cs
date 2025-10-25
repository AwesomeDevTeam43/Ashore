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

        // Add a button to the Inspector
        if (GUILayout.Button("Generate GUID"))
        {
            // Tell the component to generate a new GUID
            guidComponent.GenerateGuid();

            // Mark the object as "dirty" to ensure the change is saved
            EditorUtility.SetDirty(guidComponent);
        }
    }
}