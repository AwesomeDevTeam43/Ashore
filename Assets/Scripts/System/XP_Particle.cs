using UnityEngine;

public class XP_Particle : MonoBehaviour
{
    [SerializeField] private int xpValue = 2;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Something collided with XP particle: {other.name}");
        
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Player tag detected!");
            
            XP_System playerXpSystem = other.GetComponent<XP_System>();

            if (playerXpSystem != null)
            {
                Debug.Log($"XP System found! Adding {xpValue} XP");
                playerXpSystem.IncreaseXP(xpValue);
                PlayCollectionEffect();
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError("XP_System component NOT found on player!");
            }
        }
        else
        {
            Debug.Log($"Wrong tag detected: {other.tag}, expected: {playerTag}");
        }
    }

    private void PlayCollectionEffect()
    {
        Debug.Log($"Collected {xpValue} XP!");
    }
}