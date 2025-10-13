using UnityEngine;

public class Heal_Plant : MonoBehaviour
{
    private GameObject player;
    private Player_InputHandler player_InputHandler;
    private Player_Controller player_Controller;

    [SerializeField] private Sprite plant_dead;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private GameObject heal_particle;


    [SerializeField] private bool isDead = false;
    private bool playerNearby = false;

    void Start()
    {
        // Find player and validate
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError($"Heal_Plant on {gameObject.name}: Player not found with tag 'Player'!");
            enabled = false;
            return;
        }

        // Get components and validate
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"Heal_Plant on {gameObject.name}: SpriteRenderer component missing!");
        }

        player_InputHandler = player.GetComponent<Player_InputHandler>();
        if (player_InputHandler == null)
        {
            Debug.LogError($"Heal_Plant on {gameObject.name}: Player_InputHandler component not found on player!");
            enabled = false;
            return;
        }

        player_Controller = player.GetComponent<Player_Controller>();
        if (player_Controller == null)
        {
            Debug.LogError($"Heal_Plant on {gameObject.name}: Player_Controller component not found on player!");
            enabled = false;
            return;
        }

        // Validate prefab assignment
        if (heal_particle == null)
        {
            Debug.LogWarning($"Heal_Plant on {gameObject.name}: Heal particle prefab is not assigned!");
        }

        if (plant_dead == null)
        {
            Debug.LogWarning($"Heal_Plant on {gameObject.name}: Plant dead sprite is not assigned!");
        }

        Debug.Log($"Heal_Plant on {gameObject.name}: Successfully initialized!");
    }

    private void Update()
    {
        // Add null checks to prevent errors
        if (player == null || player_Controller == null || player_InputHandler == null)
            return;

    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("colidde");
            if (Input.GetKeyDown(KeyCode.E))
            {
                DropHeal();
            }
        }
    }


    private void DropHeal()
    {
        if (!isDead)
        {
            Debug.Log($"DropHeal called on {gameObject.name}");

            if (heal_particle == null)
            {
                Debug.LogWarning($"Heal_Plant on {gameObject.name}: Heal Particle prefab is not assigned!");
                return;
            }

            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f
            );

            Vector3 spawnPosition = transform.position + randomOffset;

            GameObject healParticle = Instantiate(heal_particle, spawnPosition, Quaternion.identity);

            Rigidbody2D rb = healParticle.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomForce = new Vector2(
                    UnityEngine.Random.Range(-2f, 2f),
                    UnityEngine.Random.Range(1f, 4f)
                );
                rb.AddForce(randomForce, ForceMode2D.Impulse);
            }

            isDead = true;
            if (spriteRenderer != null && plant_dead != null)
            {
                spriteRenderer.sprite = plant_dead;
            }

            Debug.Log($"Heal plant {gameObject.name} is now dead");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}