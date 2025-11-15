using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.InputSystem;
using System.Reflection;

public class Heal_Plant : MonoBehaviour
{
    private GameObject player;
    private Player_InputHandler player_InputHandler;
    private Player_Controller player_Controller;

    // reflection + action hook (we don't modify Player_InputHandler)
    private InputAction playerInteractAction;
    private System.Action<InputAction.CallbackContext> interactCallback;

    [SerializeField] private Sprite plant_dead;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private int heal_amount = 3;
    [SerializeField] private GameObject heal_particle;


    [SerializeField] private bool isDead = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (player != null)
        {
            player_InputHandler = player.GetComponent<Player_InputHandler>();
            player_Controller = player.GetComponent<Player_Controller>();
        }
    }

    private void Update()
    {
        if (player == null || player_Controller == null || player_InputHandler == null)
            return;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        TrySubscribeInteract(collision.gameObject);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        TryUnsubscribeInteract();
    }

    // Keep Stay for fallback keyboard input if reflection fails
    void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        // if we have no InputAction subscription, allow keyboard fallback
        if (playerInteractAction == null)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                DropHeal();
        }
    }

    private void TrySubscribeInteract(GameObject playerObj)
    {
        TryUnsubscribeInteract();
        player_InputHandler = playerObj.GetComponent<Player_InputHandler>();
        if (player_InputHandler == null) return;

        // reflectively get private field "interactAction"
        var field = typeof(Player_InputHandler).GetField("interactAction", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return;
        var action = field.GetValue(player_InputHandler) as InputAction;
        if (action == null) return;

        playerInteractAction = action;
        interactCallback = ctx => { if (ctx.performed) DropHeal(); };
        playerInteractAction.performed += interactCallback;
    }

    private void TryUnsubscribeInteract()
    {
        if (playerInteractAction != null && interactCallback != null)
        {
            playerInteractAction.performed -= interactCallback;
        }
        playerInteractAction = null;
        interactCallback = null;
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