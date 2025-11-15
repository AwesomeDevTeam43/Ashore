using System.Collections;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private bool damageable = true;
    [SerializeField] private float invincibilityDuration = .2f;

    private bool hit;
    private HealthSystem healthSystem;
    private XP_System xP_System;
    private Drop_Materials drop_Materials;
    private Drop_Item drop_Item;
    private DropEquipment drop_Equipment;

    private int xpOnDeath;
    private int dropA;
    private int dropB;
    private int dropC;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        drop_Materials = GetComponent<Drop_Materials>();
        drop_Item = GetComponent<Drop_Item>();
        drop_Equipment = GetComponent<DropEquipment>();
    }

    private void Start()
    {
        xP_System = GameObject.FindGameObjectWithTag("Player")?.GetComponent<XP_System>();

        if (healthSystem == null)
        {
            Debug.LogError($"{name}: missing HealthSystem component.");
            return;
        }

        healthSystem.Initialize(maxHealth);
        healthSystem.OnHealthChanged += OnEnemyHealthChanged;
    }

    public void Initialize(int maxHp, int xpOnDeath, int dropA, int dropB, int dropC)
    {
        this.maxHealth = maxHp;
        this.xpOnDeath = xpOnDeath;
        this.dropA = dropA;
        this.dropB = dropB;
        this.dropC = dropC;

        if (healthSystem != null)
        {
            healthSystem.Initialize(this.maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!damageable || hit || healthSystem == null || healthSystem.CurrentHealth <= 0) return;

        hit = true;
        healthSystem.TakeDamage(damage);
        StartCoroutine(TurnOffHit());
    }

    private void OnEnemyHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0)
        {
            if (xP_System != null)
            {
                xP_System.DropXP(transform.position, xpOnDeath);
            }

            if (drop_Materials != null)
            {
                drop_Materials.DropMaterial(dropA, dropB, dropC);
            }

            if (drop_Item != null)
            {
                drop_Item.DropItem();
            }

            if (drop_Equipment != null)
            {
                drop_Equipment.Drop();
            }

            Destroy(gameObject);
        }
    }

    private IEnumerator TurnOffHit()
    {
        yield return new WaitForSeconds(invincibilityDuration);
        hit = false;
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= OnEnemyHealthChanged;
    }
}