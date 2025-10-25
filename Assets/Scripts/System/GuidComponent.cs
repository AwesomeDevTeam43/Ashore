// In GuidComponent.cs

using UnityEngine;

[System.Serializable]
public class GuidComponent : MonoBehaviour
{
    [SerializeField]
    private string uniqueId;

    public string GetGuid()
    {
        return uniqueId;
    }

    // This will be called by our new editor script.
    public void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }
}