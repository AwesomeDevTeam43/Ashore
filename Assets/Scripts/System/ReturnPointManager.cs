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

  public static void StartTracking(DynamicReturnPoint trigger)
  {
    Instance.activeTrigger = trigger;
  }

  public static void SetReturnPoint(Vector3 position)
  {

    if (Instance.activeTrigger != null)
    {
      Instance.returnPoint = position;
    }
  }
      public static void StopTracking(DynamicReturnPoint trigger)
    {
        // Only stop if this is the currently active trigger
        if (Instance.activeTrigger == trigger)
        {
            Instance.activeTrigger = null;
            // Return point stays at the last position where player was in the trigger
        }
    }

  public static Vector3 GetReturnPoint()
  {
    return Instance.returnPoint;
  }

}
