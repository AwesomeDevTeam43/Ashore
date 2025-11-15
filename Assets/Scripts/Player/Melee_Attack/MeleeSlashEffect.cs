using UnityEngine;
using System.Collections;

/// <summary>
/// Runtime controller for a melee slash visual.
/// Responsibilities:
///  - Applies cardinal orientation & offset provided by weapon.
///  - Optional fade / scale tween over lifetime.
///  - Self-destroys at end.
/// Assumes it is parented under the attack origin when spawned.
/// </summary>
[DisallowMultipleComponent]
public class MeleeSlashEffect : MonoBehaviour
{
    [Header("Auto Lifetime")]
    [SerializeField] private float lifetime = 0.25f;
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0,1,1,0);
    [Header("Scale Tween (optional)")]
    [SerializeField] private bool scaleTween = true;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0,0.8f,1,1f);

    private float elapsed;
    private SpriteRenderer sr;
    private Vector3 initialLocalScale;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        initialLocalScale = transform.localScale;
    }

    public void Initialize(Vector2 localOffset, float rotationZ, bool flipX, float overrideLifetime = -1f)
    {
        transform.localPosition = (Vector3)localOffset;
        transform.localRotation = Quaternion.Euler(0,0,rotationZ);
        if (sr != null) sr.flipX = flipX;
        if (overrideLifetime > 0f) lifetime = overrideLifetime;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime; // unaffected by pause if timescale 0
        float t = Mathf.Clamp01(elapsed / lifetime);
        if (fadeOut && sr != null)
        {
            float a = fadeCurve.Evaluate(t);
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
        if (scaleTween)
        {
            float s = scaleCurve.Evaluate(t);
            transform.localScale = initialLocalScale * s;
        }
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
