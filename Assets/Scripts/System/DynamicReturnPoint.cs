using UnityEngine;

public class DynamicReturnPoint : MonoBehaviour
{
   void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Start tracking player position while in this trigger
            ReturnPointManager.StartTracking(this);
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Continuously update return point to player's current position
            ReturnPointManager.SetReturnPoint(other.transform.position);
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Stop tracking when player leaves
            ReturnPointManager.StopTracking(this);
        }
    }
}
