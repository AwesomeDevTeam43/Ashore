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
    private int woodDrop;
    private int stoneDrop;
    private int ropeDrop;

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

    public void Initialize(int maxHp, int xpOnDeath, int woodDrop, int stoneDrop, int ropeDrop)
    {
        this.maxHealth = maxHp;
        this.xpOnDeath = xpOnDeath;
        this.woodDrop = woodDrop;
        this.stoneDrop = stoneDrop;
        this.ropeDrop = ropeDrop;

        if (healthSystem != null)
        {
            healthSystem.Initialize(this.maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!damageable || hit || healthSystem == null || healthSystem.CurrentHealth <= 0) return;

        // Optionally compute a minimum allowed health (clamp) for special enemies (e.g., boss thresholds).
        int? minAllowed = null;
        var boss = GetComponent<OldFriend_Boss>();
        if (boss != null)
        {
            minAllowed = boss.GetHealthClampForIncomingDamage(damage);
        }

        hit = true;
        if (minAllowed.HasValue)
        {
            healthSystem.TakeDamage(damage, null, minAllowed.Value);
        }
        else
        {
            healthSystem.TakeDamage(damage);
        }

        StartCoroutine(TurnOffHit());
    }

    public void SetDamageable(bool value)
    {
        damageable = value;
    }

    public bool IsDamageable => damageable;

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
                drop_Materials.DropMaterial(woodDrop, stoneDrop, ropeDrop);
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