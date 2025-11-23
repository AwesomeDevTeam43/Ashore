using System.Collections;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private bool damageable = true;
    [SerializeField] private float invincibilityDuration = .2f;
    [SerializeField, Range(0f, 1f)] private float meleeResistance;
    [SerializeField, Range(0f, 1f)] private float rangedResistance;

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
    private float pendingDamageBuffer;

    public enum DamageSourceType
    {
        Generic,
        PlayerMelee,
        PlayerRanged
    }

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

    public void Initialize(int maxHp, int xpOnDeath, int woodDrop, int stoneDrop, int ropeDrop, float meleeResistance = 0f, float rangedResistance = 0f)
    {
        this.maxHealth = maxHp;
        this.xpOnDeath = xpOnDeath;
        this.woodDrop = woodDrop;
        this.stoneDrop = stoneDrop;
        this.ropeDrop = ropeDrop;
        this.meleeResistance = Mathf.Clamp01(meleeResistance);
        this.rangedResistance = Mathf.Clamp01(rangedResistance);
        pendingDamageBuffer = 0f;

        if (healthSystem != null)
        {
            healthSystem.Initialize(this.maxHealth);
        }
    }

    public void TakeDamage(float damage, DamageSourceType damageSource = DamageSourceType.Generic)
    {
        if (!damageable || hit || healthSystem == null || healthSystem.CurrentHealth <= 0) return;

        float scaledDamage = ApplyResistance(damage, damageSource);
        if (scaledDamage <= 0f)
        {
            Debug.Log($"{name} ignored damage due to resistances against {damageSource} attacks.");
            return;
        }

        // Optionally compute a minimum allowed health (clamp) for special enemies (e.g., boss thresholds).
        int? minAllowed = null;
        var boss = GetComponent<OldFriend_Boss>();
        if (boss != null)
        {
            minAllowed = boss.GetHealthClampForIncomingDamage(Mathf.CeilToInt(scaledDamage));
        }

        hit = true;
        ApplyBufferedDamage(scaledDamage, minAllowed);

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

    private float ApplyResistance(float incomingDamage, DamageSourceType damageSource)
    {
        if (incomingDamage <= 0f) return 0f;

        float reduction = 0f;
        switch (damageSource)
        {
            case DamageSourceType.PlayerMelee:
                reduction = meleeResistance;
                break;
            case DamageSourceType.PlayerRanged:
                reduction = rangedResistance;
                break;
            default:
                return incomingDamage;
        }

        float reducedPercent = Mathf.Clamp01(1f - Mathf.Clamp01(reduction));
        return incomingDamage * reducedPercent;
    }

    private void ApplyBufferedDamage(float scaledDamage, int? minAllowed)
    {
        if (scaledDamage <= 0f) return;

        pendingDamageBuffer += scaledDamage;
        int wholeDamage = Mathf.FloorToInt(pendingDamageBuffer);
        if (wholeDamage <= 0) return;

        int currentHealth = healthSystem.CurrentHealth;
        int minClamp = minAllowed ?? int.MinValue;
        if (minClamp != int.MinValue)
        {
            int maxAllowedLoss = Mathf.Max(0, currentHealth - minClamp);
            if (maxAllowedLoss <= 0)
            {
                pendingDamageBuffer = Mathf.Min(pendingDamageBuffer, 0.999f);
                return;
            }
            if (wholeDamage > maxAllowedLoss)
            {
                int prevented = wholeDamage - maxAllowedLoss;
                wholeDamage = maxAllowedLoss;
                pendingDamageBuffer -= prevented;
            }
        }

        pendingDamageBuffer -= wholeDamage;
        healthSystem.TakeDamage(wholeDamage);
    }

    public void RestoreFullHealth()
    {
        pendingDamageBuffer = 0f;
        if (healthSystem != null)
        {
            healthSystem.SetHealth(maxHealth);
        }
    }
}