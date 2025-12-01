using UnityEngine;
using System.Collections;

public class EnemyCombatFeedback : MonoBehaviour
{
    [Header("Hit Flash")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.1f;

    [Header("Shake on Hit")]
    [SerializeField] private bool shakeOnHit = true;
    [SerializeField] private float shakeIntensity = 0.15f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Attack Telegraph")]
    [SerializeField] private Color attackTelegraphColor = new Color(1f, 0.5f, 0f, 0.7f);
    [SerializeField] private float telegraphDuration = 0.3f;
    [SerializeField] private bool pulseWhileTelegraphing = true;
    [SerializeField] private float pulseCycleTime = 0.15f;

    [Header("Particles")]
    [SerializeField] private GameObject hitParticlePrefab;
    [SerializeField] private GameObject attackParticlePrefab;
    [SerializeField] private GameObject deathParticlePrefab;
    [SerializeField] private GameObject critHitParticlePrefab;

    [Header("Critical Hit Feedback")]
    [SerializeField] private bool criticalFeedbackEnabled = true;
    [SerializeField] private Color critFlashColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private float critFlashDuration = 0.15f;
    [SerializeField] private float critShakeIntensityMultiplier = 1.5f;
    [SerializeField] private float critShakeDurationMultiplier = 1.1f;
    [SerializeField] private GameObject critParticlePrefab;

    private Color[] originalColors;
    private Vector3 originalPosition;
    private Coroutine currentTelegraphRoutine;

    private void Awake()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                originalColors[i] = spriteRenderers[i].color;
        }

        originalPosition = transform.position;
    }

    // Chamar quando inimigo toma dano
    public void PlayHitFeedback(Vector2 hitPoint, bool isCritical = false)
    {
        StartCoroutine(SpriteFlash(hitFlashColor, hitFlashDuration));

        if (shakeOnHit)
            StartCoroutine(ShakeRoutine(shakeIntensity, shakeDuration));

        if (hitParticlePrefab != null)
            Instantiate(hitParticlePrefab, hitPoint, Quaternion.identity);

        if (isCritical && critHitParticlePrefab != null)
            Instantiate(critHitParticlePrefab, hitPoint, Quaternion.identity);
    }

    // Chamar quando o jogador acerta um ataque crítico
    public void PlayCriticalHitFeedback(Vector2 hitPoint)
    {
        if (!criticalFeedbackEnabled)
        {
            PlayHitFeedback(hitPoint);
            return;
        }

        StartCoroutine(SpriteFlash(critFlashColor, critFlashDuration));

        if (shakeOnHit)
        {
            float critIntensity = shakeIntensity * critShakeIntensityMultiplier;
            float critDuration = shakeDuration * critShakeDurationMultiplier;
            StartCoroutine(ShakeRoutine(critIntensity, critDuration));
        }

        GameObject prefab = critParticlePrefab != null ? critParticlePrefab : hitParticlePrefab;
        if (prefab != null)
        {
            Instantiate(prefab, hitPoint, Quaternion.identity);
            Debug.Log("aqui");
        }

    }

    // Chamar antes de atacar (telegraph)
    public void PlayAttackTelegraph()
    {
        if (currentTelegraphRoutine != null)
            StopCoroutine(currentTelegraphRoutine);
        currentTelegraphRoutine = StartCoroutine(AttackTelegraph());
    }

    // Chamar quando ataque é executado
    public void PlayAttackFeedback(Vector2 attackPoint)
    {
        if (attackParticlePrefab != null)
            Instantiate(attackParticlePrefab, attackPoint, Quaternion.identity);
    }

    // Chamar quando inimigo morre
    public void PlayDeathFeedback()
    {
        if (deathParticlePrefab != null)
            Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
    }

    private IEnumerator SpriteFlash(Color flashColor, float duration)
    {
        // Flash para vermelho
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = flashColor;
        }

        yield return new WaitForSeconds(duration);

        // Voltar para cor original
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalColors[i];
        }
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            transform.position = startPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;
    }

    private IEnumerator AttackTelegraph()
    {
        float elapsed = 0f;

        while (elapsed < telegraphDuration)
        {
            float t = elapsed / telegraphDuration;
            Color targetColor = attackTelegraphColor;

            if (pulseWhileTelegraphing)
            {
                float pulse = Mathf.PingPong(elapsed / pulseCycleTime, 1f);
                targetColor = Color.Lerp(originalColors[0], attackTelegraphColor, pulse);
            }

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    spriteRenderers[i].color = Color.Lerp(originalColors[i], targetColor, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restaurar cores
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalColors[i];
        }
    }
}
