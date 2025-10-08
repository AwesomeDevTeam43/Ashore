using UnityEngine;

public class Heal_Plant : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private Player_InputHandler player_InputHandler;
    private Player_Controller player_Controller;

    [SerializeField] private Sprite plant_dead;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private GameObject heal_particle;

    private bool isDead = false;
    private bool playerNearby = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player_InputHandler = player.GetComponent<Player_InputHandler>();
        player_Controller = player.GetComponent<Player_Controller>();
    }

    private void Update()
    {
        if (player_Controller.IsAlive)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            playerNearby = distance <= 1f;

            if (playerNearby && player_InputHandler.InteractActionTriggered)
            {
                DropHeal();
            }
        }
    }

    private void DropHeal()
    {
        if (!isDead)
        {
            if (heal_particle == null)
            {
                Debug.LogWarning("XP Particle prefab is not assigned!");
                return;
            }

            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f
            );

            Vector3 spawnPosition = transform.position + randomOffset;

            GameObject xpParticle = Instantiate(heal_particle, spawnPosition, Quaternion.identity);

            Rigidbody2D rb = xpParticle.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomForce = new Vector2(
                    UnityEngine.Random.Range(-2f, 2f),
                    UnityEngine.Random.Range(1f, 4f)
                );
                rb.AddForce(randomForce, ForceMode2D.Impulse);
            }

            isDead = true;
            spriteRenderer.sprite = plant_dead;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
