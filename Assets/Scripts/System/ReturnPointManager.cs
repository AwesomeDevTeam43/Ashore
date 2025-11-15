using UnityEngine;

public class ReturnPointManager : MonoBehaviour
{
  public static ReturnPointManager Instance;
  private Vector3 returnPoint;
  private DynamicReturnPoint activeTrigger;

  void Awake()
  {
    Instance = this;
    returnPoint = transform.position;
  }

  void OnDestroy()
  {
    if (Instance == this) Instance = null;
  }

  public static void StartTracking(DynamicReturnPoint trigger)
  {
    if (Instance == null || trigger == null) return;
    Instance.activeTrigger = trigger;
    Instance.returnPoint = trigger.GetRespawnPosition();
  }

  public static void SetReturnPoint(Vector3 position)
  {
    if (Instance == null) return;
    if (Instance.activeTrigger != null)
    {
      Instance.returnPoint = position;
    }
  }

  public static void StopTracking(DynamicReturnPoint trigger)
  {
    if (Instance == null || trigger == null) return;
    if (Instance.activeTrigger == trigger)
    {
      Instance.activeTrigger = null;
    }
  }

  public static Vector3 GetReturnPoint()
  {
    if (Instance == null) return Vector3.zero;
    return Instance.returnPoint;
  }
}
