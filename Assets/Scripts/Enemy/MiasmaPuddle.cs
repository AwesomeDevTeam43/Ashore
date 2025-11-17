using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MiasmaPuddle : MonoBehaviour
{
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private bool destroyOnPlayerExit = false;

    private readonly Dictionary<Collider2D, float> _nextTickByCollider = new();
    private float _despawnTime;

    private void OnEnable()
    {
        _despawnTime = Time.time + lifetime;
    }

    private void Update()
    {
        if (Time.time >= _despawnTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!_nextTickByCollider.TryGetValue(other, out var nextTick) || Time.time >= nextTick)
        {
            ApplyDamage(other);
            _nextTickByCollider[other] = Time.time + tickInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        _nextTickByCollider.Remove(other);
        if (destroyOnPlayerExit && _nextTickByCollider.Count == 0)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyDamage(Collider2D target)
    {
        var dmg = target.GetComponentInParent<IDamageReceiver>();
        if (dmg != null)
        {
            dmg.ReceiveDamage(damagePerTick);
            return;
        }

        var healthSystem = target.GetComponentInParent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damagePerTick, gameObject);
            return;
        }

        var playerHealth = target.GetComponentInParent<Player_Health>();
        if (playerHealth != null)
        {
            var hs = playerHealth.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.TakeDamage(damagePerTick, gameObject);
                return;
            }
        }

        var behaviours = target.GetComponentsInParent<MonoBehaviour>();
        foreach (var mb in behaviours)
        {
            if (mb == null) continue;
            var method = mb.GetType().GetMethod("TakeDamage", new[] { typeof(int) });
            if (method != null)
            {
                method.Invoke(mb, new object[] { damagePerTick });
                return;
            }
        }
    }

    private void OnDisable()
    {
        _nextTickByCollider.Clear();
    }
}
