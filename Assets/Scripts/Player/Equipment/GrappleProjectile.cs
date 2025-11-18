using UnityEngine;

public class GrappleProjectile : MonoBehaviour
{
    [SerializeField] private GameObject grappleAnchorPrefab;
    [SerializeField] private LayerMask attachableLayers;
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float anchorSpawnOffset = 0.06f; // push anchor slightly away from surface to avoid being inside geometry
    private bool registeredWithTracker = false;

    private void Start()
    {
        // Basic self-destruction timer
        Destroy(gameObject, lifeTime);

        // Diagnostic logs to help debug why anchors don't spawn.
        Collider2D col = GetComponent<Collider2D>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Debug.LogFormat("GrappleProjectile.Start: layer={0}, attachableLayers={1}, hasCollider={2}, colliderIsTrigger={3}, hasRigidbody={4}",
            LayerMask.LayerToName(gameObject.layer), attachableLayers.value, col != null, col != null ? col.isTrigger.ToString() : "n/a", rb != null);
    }

    private void OnEnable()
    {
        GrappleInstanceRegistry.RegisterProjectile();
        registeredWithTracker = true;
    }

    private void OnDestroy()
    {
        if (registeredWithTracker)
        {
            GrappleInstanceRegistry.UnregisterProjectile();
            registeredWithTracker = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("GrappleProjectile.OnCollisionEnter2D with " + LayerMask.LayerToName(collision.gameObject.layer));
        // Ignore hitting the player so the projectile can pass by the thrower
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }
        // Determine whether this collision should spawn an anchor.
        // If the attachableLayers mask is left as the default/empty (0) in the inspector,
        // treat that as "attach to any non-player, non-enemy collider" for convenience.
        bool maskIsEmpty = attachableLayers.value == 0;
        bool layerMatches = maskIsEmpty || (((1 << collision.gameObject.layer) & attachableLayers.value) != 0);
        if (layerMatches)
        {
            Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            Vector2 normal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector2.up;
            SpawnAnchorAt(hitPoint, normal);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // ignore enemy collisions and keep flying
            return;
        }

        // Helpful debug when anchor isn't spawned
        if (!layerMatches)
        {
            Debug.Log("GrappleProjectile: collision with layer '" + LayerMask.LayerToName(collision.gameObject.layer) + "' did not match attachableLayers mask.");
        }

        // Destroy projectile after attempt to attach
        Destroy(gameObject);
    }

    // Support trigger colliders as well (some prefabs may use triggers)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("GrappleProjectile.OnTriggerEnter2D with " + LayerMask.LayerToName(collision.gameObject.layer));

        // Ignore player
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        bool maskIsEmpty = attachableLayers.value == 0;
        bool layerMatches = maskIsEmpty || (((1 << collision.gameObject.layer) & attachableLayers.value) != 0);

        if (layerMatches)
        {
            Vector3 hitPoint = collision.ClosestPoint(transform.position);
            // approximate normal away from the collision surface by using vector from contact to projectile
            Vector2 normal = (transform.position - hitPoint).normalized;
            if (normal == Vector2.zero) normal = Vector2.up;
            SpawnAnchorAt(hitPoint, normal);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // ignore enemy collisions and keep flying
            return;
        }

        if (!layerMatches)
        {
            Debug.Log("GrappleProjectile: trigger with layer '" + LayerMask.LayerToName(collision.gameObject.layer) + "' did not match attachableLayers mask.");
        }

        Destroy(gameObject);
    }

    private void SpawnAnchorAt(Vector3 position, Vector2 normal)
    {
        Vector3 spawnPos = position + (Vector3)(normal.normalized * anchorSpawnOffset);
        if (grappleAnchorPrefab != null)
        {
            Instantiate(grappleAnchorPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("GrappleProjectile: grappleAnchorPrefab is not assigned.");
        }
    }
}
