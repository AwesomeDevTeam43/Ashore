using UnityEngine;

public class Map_PlatformMoves : MonoBehaviour
{
    [Header("Platform Parameters")]
    [SerializeField] public float amplitude = 3f;
    [SerializeField] public float speed = 2f;
    private Vector3 startPos;
    [SerializeField] public bool vertical = false;
    [SerializeField] public bool horizontal = false;
    private float movementTime = 0f;

    [SerializeField] public bool requirePlayer = false;
    private bool canMove = true;
    private bool playerOnTop = false;
    private Collider2D platformCollider;
    private Rigidbody2D rb;

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

        canMove = !requirePlayer || playerOnTop;
    }

    void FixedUpdate()
    {
        if (!canMove) return;

        movementTime += Time.fixedDeltaTime;

        Vector3 target = transform.position;

        if (vertical)
        {
            float newY = startPos.y + Mathf.Sin(movementTime * speed) * amplitude;
            target = new Vector3(startPos.x, newY, startPos.z);
        }
        else if (horizontal)
        {
            float newX = startPos.x + Mathf.Sin(movementTime * speed) * amplitude;
            target = new Vector3(newX, startPos.y, startPos.z);
        }

        rb.MovePosition(target);
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