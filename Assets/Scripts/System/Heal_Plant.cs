using UnityEngine;
using UnityEngine.PlayerLoop;

public class Heal_Plant : MonoBehaviour
{
    private GameObject player;
    private Player_InputHandler player_InputHandler;
    private Player_Controller player_Controller;

    [SerializeField] private Sprite plant_dead;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private int heal_amount = 3;
    [SerializeField] private GameObject heal_particle;


    [SerializeField] private bool isDead = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        player_InputHandler = player.GetComponent<Player_InputHandler>();
        player_Controller = player.GetComponent<Player_Controller>();
    }

    private void Update()
    {
        if (player == null || player_Controller == null || player_InputHandler == null)
            return;
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        Debug.Log("collided with player (OnTriggerStay2D)");
        if (Input.GetKeyDown(KeyCode.E))
        {
            DropHeal();
        }
    }

    private void DropHeal()
    {
        if (!isDead)
        {
            Debug.Log($"DropHeal called on {gameObject.name}");

            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f
            );

            Vector3 spawnPosition = transform.position + randomOffset;

            GameObject healParticle = Instantiate(heal_particle, spawnPosition, Quaternion.identity);

            var healComponent = healParticle.GetComponent<Heal_Particle>();
            if (healComponent != null)
            {
                healComponent.SetHealAmount(heal_amount);
            }

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
        }
    }
}