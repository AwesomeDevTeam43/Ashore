using UnityEngine;

public class Map_PlatformMoves : MonoBehaviour
{
    public enum StartPosition { Center = 0, Min = 1, Max = 2, Random = 3 }

    [Header("Platform Parameters")]
    [SerializeField] public float amplitude = 3f;
    [SerializeField] public float speed = 2f;
    [Header("Platform Position")]
    [SerializeField] public StartPosition startPosition = StartPosition.Center;
    private Vector3 startPos;
    [SerializeField] public bool vertical = false;
    [SerializeField] public bool horizontal = false;
    private float movementTime = 0f;

    [Header("Platform Parameters")]
    [SerializeField] public bool requirePlayer = false;
    private bool canMove = true;
    private bool playerOnTop = false;
    private Collider2D platformCollider;
    private Rigidbody2D rb;

    [Header("Endpoint Pause")]
    [SerializeField] public bool pauseAtEndpoints = false;
    [SerializeField] public float endpointCooldown = 1f;
    private bool pauseActive = false;
    private float endpointPauseTimer = 0f;
    private int lastEndpoint = 0;
    private const float endpointThreshold = 0.01f;
    private const float endpointExitThreshold = 0.9f;

     void Start()
    {
        startPos = transform.position;

        platformCollider = GetComponent<Collider2D>();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;
        }

        float phase;
        switch (startPosition)
        {
            case StartPosition.Min:
                phase = -Mathf.PI / 2f;
                break;
            case StartPosition.Max:
                phase = Mathf.PI / 2f;
                break;
            case StartPosition.Random:
                phase = Random.Range(0f, Mathf.PI * 2f);
                break;
            default:
                phase = 0f;
                break;
        }

        movementTime = phase / Mathf.Max(0.0001f, speed);

        if (rb != null)
        {
            if (vertical)
            {
                float newY = startPos.y + Mathf.Sin(movementTime * speed) * amplitude;
                rb.position = new Vector2(startPos.x, newY);
                transform.position = rb.position;
            }
            else if (horizontal)
            {
                float newX = startPos.x + Mathf.Sin(movementTime * speed) * amplitude;
                rb.position = new Vector2(newX, startPos.y);
                transform.position = rb.position;
            }
        }

        canMove = !requirePlayer || playerOnTop;
    }

    void FixedUpdate()
    {
        if (pauseActive)
        {
            endpointPauseTimer -= Time.fixedDeltaTime;
            if (endpointPauseTimer <= 0f)
            {
                pauseActive = false;
                canMove = !requirePlayer || playerOnTop;
            }
            return;
        }

        if (!canMove) return;

        movementTime += Time.fixedDeltaTime;

        Vector3 target = transform.position;

        float phaseValue = Mathf.Sin(movementTime * speed);

        if (vertical)
        {
            float newY = startPos.y + phaseValue * amplitude;
            target = new Vector3(startPos.x, newY, startPos.z);
        }
        else if (horizontal)
        {
            float newX = startPos.x + phaseValue * amplitude;
            target = new Vector3(newX, startPos.y, startPos.z);
        }

        rb.MovePosition(target);

        if (pauseAtEndpoints && endpointCooldown > 0f)
        {
            float edgeDiff = Mathf.Abs(Mathf.Abs(phaseValue) - 1f);
            if (edgeDiff <= endpointThreshold)
            {
                int endpoint = phaseValue > 0f ? 1 : -1;
                if (endpoint != lastEndpoint)
                {
                    pauseActive = true;
                    endpointPauseTimer = endpointCooldown;
                    canMove = false;
                    lastEndpoint = endpoint;
                }
            }
            else if (Mathf.Abs(phaseValue) < endpointExitThreshold)
            {
                lastEndpoint = 0;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!requirePlayer) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                playerOnTop = true;
                canMove = true;
                collision.transform.SetParent(transform);
                return;
            }
        }

        var playerPos = collision.transform.position;
        if (playerPos.y > transform.position.y)
        {
            playerOnTop = true;
            canMove = true;
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!requirePlayer) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                if (collision.transform.parent != transform) collision.transform.SetParent(transform);
                playerOnTop = true;
                canMove = true;
                return;
            }
        }

        if (collision.transform.position.y > transform.position.y)
        {
            if (collision.transform.parent != transform) collision.transform.SetParent(transform);
            playerOnTop = true;
            canMove = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!requirePlayer) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        if (collision.transform.parent == transform) collision.transform.SetParent(null);
        playerOnTop = false;
        canMove = false;
    }
}