using UnityEngine;

public class Chest : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private Player_InputHandler player_InputHandler;
    private Player_Controller player_Controller;

    [Header("Chest Sprites")]
    [SerializeField] private Sprite openSprite;

    [Header("Rewards")]
    [SerializeField] private int xpReward = 1;

    private SpriteRenderer spriteRenderer;
    private XP_System xP_System;
    private bool isOpen = false;
    private bool playerNearby = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        xP_System = player.GetComponent<XP_System>();
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
                OpenChest();
            }
        }
    }

    private void OpenChest()
    {
        if (!isOpen)
        {
            isOpen = true;
            spriteRenderer.sprite = openSprite;
            xP_System.DropXP(transform.position, xpReward);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

}