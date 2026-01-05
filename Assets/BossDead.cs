using System.Collections;
using UnityEngine;

public class BossDead : MonoBehaviour
{
    [Header("Boss Reference")]
    [Tooltip("Assign the boss' HealthSystem (or assign Boss GameObject to auto-find)")]
    [SerializeField] private HealthSystem bossHealth;
    [SerializeField] private GameObject bossGameObject;

    [Header("Door Movement")]
    [Tooltip("Local upward distance the door should move when opening")]
    [SerializeField] private float raiseHeight = 5f;
    [SerializeField] private float raiseDuration = 2f;
    [SerializeField] private bool disableColliderWhenOpen = true;

    [Header("Soundtrack")]
    [SerializeField] private AudioClip soundtrack;
    [SerializeField] private AudioSource audioSource; // optional: assign a persistent music source

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool opened = false;

    private Collider2D doorCollider;

    void Start()
    {
        // resolve bossHealth from bossGameObject if needed
        if (bossHealth == null && bossGameObject != null)
        {
            bossHealth = bossGameObject.GetComponent<HealthSystem>();
        }

        // try to find a HealthSystem on the scene if none assigned (best effort)
        if (bossHealth == null && bossGameObject == null)
        {
            var bossObj = GameObject.FindFirstObjectByType<OldFriend_Boss>();
            if (bossObj != null)
                bossHealth = bossObj.GetComponent<HealthSystem>();
        }

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += OnBossHealthChanged;
        }

        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * raiseHeight;

        doorCollider = GetComponent<Collider2D>();
    }

    private void OnBossHealthChanged(int current, int max)
    {
        if (opened) return;
        if (current <= 0)
        {
            StartCoroutine(OpenDoorRoutine());
        }
    }

    private IEnumerator OpenDoorRoutine()
    {
        opened = true;

        // start soundtrack
        if (soundtrack != null)
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = true;
            }
            if (audioSource.clip == null || audioSource.clip != soundtrack)
            {
                audioSource.clip = soundtrack;
            }
            audioSource.Play();
        }

        // move door up smoothly
        float elapsed = 0f;
        Vector3 start = transform.position;
        while (elapsed < raiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / raiseDuration);
            transform.position = Vector3.Lerp(start, openPosition, t);
            yield return null;
        }
        transform.position = openPosition;

        if (disableColliderWhenOpen && doorCollider != null)
            doorCollider.enabled = false;
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChanged -= OnBossHealthChanged;
    }
}
