#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelPortal))]
public class LevelPortalEditor : Editor
{
  public override void OnInspectorGUI()
  {
    DrawDefaultInspector();

    var portal = (LevelPortal)target;

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Portal Tools", EditorStyles.boldLabel);

    using (new EditorGUILayout.HorizontalScope())
    {
      if (GUILayout.Button("Create Camera Anchor"))
      {
        CreateOrAssignAnchor(portal);
      }
      if (GUILayout.Button("Move Anchor To Stick Center"))
      {
        MoveAnchorToStickCenter(portal);
      }
    }

    using (new EditorGUILayout.HorizontalScope())
    {
      if (GUILayout.Button("SpawnOffset = Anchor Delta"))
      {
        SetSpawnOffsetFromAnchor(portal);
      }
      if (GUILayout.Button("Anchor = SpawnOffset Pos"))
      {
        SetAnchorFromSpawnOffset(portal);
      }
    }
  }

  private void CreateOrAssignAnchor(LevelPortal portal)
  {
    if (portal.cameraStickAnchor == null)
    {
      var go = new GameObject(portal.portalId + "_CamAnchor");
      go.transform.SetParent(portal.transform.parent, true);
      go.transform.position = portal.transform.position + (Vector3)portal.stickAreaCenter;
      portal.cameraStickAnchor = go.transform;
      EditorUtility.SetDirty(portal);
      Selection.activeTransform = go.transform;
    }
    else
    {
      Selection.activeTransform = portal.cameraStickAnchor;
    }
  }

  private void MoveAnchorToStickCenter(LevelPortal portal)
  {
    if (portal.cameraStickAnchor == null)
    {
      CreateOrAssignAnchor(portal);
    }
    if (portal.cameraStickAnchor != null)
    {
      Undo.RecordObject(portal.cameraStickAnchor, "Move Camera Anchor");
      portal.cameraStickAnchor.position = portal.transform.position + (Vector3)portal.stickAreaCenter;
      EditorUtility.SetDirty(portal.cameraStickAnchor);
    }
  }

  private void SetSpawnOffsetFromAnchor(LevelPortal portal)
  {
    if (portal.cameraStickAnchor == null)
    {
      EditorUtility.DisplayDialog("LevelPortal", "No camera anchor assigned.", "OK");
      return;
    }
    Undo.RecordObject(portal, "Set Spawn Offset");
    Vector2 delta = (Vector2)(portal.cameraStickAnchor.position - portal.transform.position);
    portal.spawnOffset = delta;
    EditorUtility.SetDirty(portal);
  }

  private void SetAnchorFromSpawnOffset(LevelPortal portal)
  {
    if (portal.cameraStickAnchor == null)
    {
      CreateOrAssignAnchor(portal);
    }
    if (portal.cameraStickAnchor != null)
    {
      Undo.RecordObject(portal.cameraStickAnchor, "Move Camera Anchor from SpawnOffset");
      portal.cameraStickAnchor.position = (Vector2)portal.transform.position + portal.spawnOffset;
      EditorUtility.SetDirty(portal.cameraStickAnchor);
    }
  }
}
#endif
