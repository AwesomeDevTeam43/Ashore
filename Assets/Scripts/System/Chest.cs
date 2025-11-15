using UnityEngine;

public class Chest : MonoBehaviour
{
    private GameObject player;

    [Header("Chest Sprites")]
    [SerializeField] private Sprite openSprite;

    [Header("Rewards")]
    [SerializeField] private int xpReward = 3;
    [SerializeField] private int stoneReward = 3;
    [SerializeField] private int woodReward = 3;
    [SerializeField] private int ropeReward = 3;

    private SpriteRenderer spriteRenderer;
    private XP_System xP_System;
    private bool isOpen = false;
    private Drop_Materials drop_Materials;

    private DropEquipment dropEquipment;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        xP_System = player.GetComponent<XP_System>();
        drop_Materials = GetComponent<Drop_Materials>();
    }

    void Start()
    {
        dropEquipment = this.GetComponent<DropEquipment>();
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
                dropEquipment.Drop();
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
            drop_Materials.DropMaterial(stoneReward, woodReward,ropeReward);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

}