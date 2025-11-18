using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;

[RequireComponent(typeof(GuidComponent))]
public class Chest : MonoBehaviour, ISaveable
{
    private GameObject player;
    private Player_InputHandler player_InputHandler;

    [Header("Chest Sprites")]
    [SerializeField] private Sprite openSprite;

    [Header("Rewards")]
    [SerializeField] private int xpReward = 3;
    [SerializeField] private int stoneReward = 3;
    [SerializeField] private int woodReward = 3;
    [SerializeField] private int ropeReward = 3;

    private SpriteRenderer spriteRenderer;
    private Sprite closedSprite;
    private XP_System xP_System;
    private bool isOpen = false;
    private Drop_Materials drop_Materials;

    private DropEquipment dropEquipment;
    private GuidComponent guidComponent;

    // reflection + action hook (non-invasive to Player_InputHandler)
    private InputAction playerInteractAction;
    private System.Action<InputAction.CallbackContext> interactCallback;
    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            closedSprite = spriteRenderer.sprite;
        }
        xP_System = player.GetComponent<XP_System>();
        drop_Materials = GetComponent<Drop_Materials>();
        if (player != null) player_InputHandler = player.GetComponent<Player_InputHandler>();
        guidComponent = GetComponent<GuidComponent>();
    }

    void Start()
    {
        dropEquipment = this.GetComponent<DropEquipment>();
        UpdateChestVisual();
    }

    private void Update()
    {

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

    void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        // fallback: if reflection subscription failed, allow keyboard E
        if (playerInteractAction == null)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryOpenFromInput();
        }
    }

    private void TrySubscribeInteract(GameObject playerObj)
    {
        TryUnsubscribeInteract();
        player_InputHandler = playerObj.GetComponent<Player_InputHandler>();
        if (player_InputHandler == null) return;

        var field = typeof(Player_InputHandler).GetField("interactAction", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return;
        var action = field.GetValue(player_InputHandler) as InputAction;
        if (action == null) return;

        playerInteractAction = action;
        interactCallback = ctx => { if (ctx.performed) TryOpenFromInput(); };
        playerInteractAction.performed += interactCallback;
    }

    private void TryUnsubscribeInteract()
    {
        if (playerInteractAction != null && interactCallback != null)
            playerInteractAction.performed -= interactCallback;
        playerInteractAction = null;
        interactCallback = null;
    }

    private void TryOpenFromInput()
    {
        if (!isOpen)
        {
            OpenChest();
            dropEquipment?.Drop();
        }
    }

    private void OpenChest()
    {
        if (!isOpen)
        {
            isOpen = true;
            UpdateChestVisual();
            xP_System.DropXP(transform.position, xpReward);
            drop_Materials?.DropMaterial(stoneReward, woodReward, ropeReward);
        }
    }

    private void UpdateChestVisual()
    {
        if (spriteRenderer == null) return;
        if (isOpen)
        {
            spriteRenderer.sprite = openSprite;
        }
        else if (closedSprite != null)
        {
            spriteRenderer.sprite = closedSprite;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

    public object CaptureState()
    {
        return isOpen;
    }

    public void RestoreState(object state)
    {
        if (state is bool savedOpenState)
        {
            isOpen = savedOpenState;
            UpdateChestVisual();
        }
    }
}