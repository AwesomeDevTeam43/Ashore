using System;
using UnityEngine;

[RequireComponent(typeof(Enemy_Health))]
public class ShieldedCore : MonoBehaviour
{
	public int coreMaxHealth = 2;

	private Enemy_Health enemyHealth;
	private SpriteRenderer[] spriteRenderers;
	private Color[] originalColors;

	private bool isProtectedState = false;

	public event Action<ShieldedCore> OnCoreDestroyed;

	private void Awake()
	{
		enemyHealth = GetComponent<Enemy_Health>();
		if (enemyHealth != null)
		{
			enemyHealth.Initialize(coreMaxHealth, 0, 0, 0, 0, 0f, 0f);
		}

		// cache sprite renderers to change color when protected
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
		if (spriteRenderers != null && spriteRenderers.Length > 0)
		{
			originalColors = new Color[spriteRenderers.Length];
			for (int i = 0; i < spriteRenderers.Length; i++)
			{
				if (spriteRenderers[i] != null)
					originalColors[i] = spriteRenderers[i].color;
			}
		}
			// ensure core starts protected by default
			SetProtected(true);
		}

	public void SetProtected(bool isProtected)
	{
		if (enemyHealth != null)
		{
			// when protected, enemyHealth should NOT be damageable
			enemyHealth.SetDamageable(!isProtected);
		}

		if (isProtectedState == isProtected) return;
		isProtectedState = isProtected;

		// update visuals: keep original color when protected, show red when unprotected
		if (spriteRenderers != null)
		{
			for (int i = 0; i < spriteRenderers.Length; i++)
			{
				var sr = spriteRenderers[i];
				if (sr == null) continue;

				if (isProtected)
				{
					// restore original color if we cached it, otherwise leave as-is
					if (originalColors != null && i < originalColors.Length)
						sr.color = originalColors[i];
					else
						sr.color = Color.white;
				}
				else
				{
					sr.color = Color.red;
				}
			}
		}
	}

	private void OnDestroy()
	{
		OnCoreDestroyed?.Invoke(this);
	}
}
