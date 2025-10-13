using UnityEngine;

public class Chest : MonoBehaviour
{
    private GameObject player;
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

    private Drop_Materials drop_Materials;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        xP_System = player.GetComponent<XP_System>();
        player_InputHandler = player.GetComponent<Player_InputHandler>();
        player_Controller = player.GetComponent<Player_Controller>();
        drop_Materials = GetComponent<Drop_Materials>();
    }

    private void Update()
    {

    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("colidde");
            if (Input.GetKeyDown(KeyCode.E))
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
            drop_Materials.DropMaterial(1, 2, 3);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

}