using UnityEngine;
using System.Collections;

public class GrappleAnchor : MonoBehaviour
{
    [SerializeField] private float ropeLifetime = 6f;
    [SerializeField] private float climbSpeed = 8f;
    [SerializeField] private float climbHorizontalTolerance = 0.6f; // how far away horizontally player can be to grab rope
    [SerializeField] private float maxClimbHeightOffset = 0.5f; // how close to anchor's y the player can get
    [Header("Rope Visual")]
    [SerializeField] private float ropeWidth = 0.08f;
    [SerializeField] private float ropeDropSpeed = 6f; // units per second while dropping
    [SerializeField] private LayerMask ropeStopLayers = ~0; // which layers the rope should stop at (default all)
    [SerializeField] private float maxRopeLength = 12f;
    [Header("Fallback")]
    [SerializeField] private float minRopeLength = 0.5f; // if computed rope length is shorter, consider it not attachable
    [SerializeField] private GameObject fallbackPickupPrefab; // optional prefab to spawn so player can pick the hook back up

    private Transform player;
    private Player_InputHandler inputHandler;
    private Rigidbody2D playerRb;

    private bool active = true;
    private GameObject ropeGO;
    private float currentRopeLength = 0f;
    private float targetRopeLength = 0f;
    // gravity handling while attached
    private float originalPlayerGravity = 1f;
    private bool gravityModified = false;

    private void Start()
    {
        Destroy(gameObject, ropeLifetime);
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            inputHandler = player.GetComponent<Player_InputHandler>();
            playerRb = player.GetComponent<Rigidbody2D>();
        }

        // Create rope visual and compute target length by raycasting downward
        CreateRopeVisual();
    }

    private void CreateRopeVisual()
    {
        Vector2 origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, maxRopeLength, ropeStopLayers);
        if (hit.collider != null)
        {
            targetRopeLength = hit.distance;
        }
        else
        {
            targetRopeLength = maxRopeLength;
        }

        // If there's not enough vertical space below the anchor, don't create a climbable rope.
        if (targetRopeLength < minRopeLength)
        {
            Debug.Log("GrappleAnchor: not enough space below anchor (" + targetRopeLength + "), cancelling anchor.");
            if (fallbackPickupPrefab != null)
            {
                Instantiate(fallbackPickupPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
            return;
        }

        // create a simple white 1x1 sprite at runtime and use it as rope
        ropeGO = new GameObject("GrappleRope");
        ropeGO.transform.SetParent(transform, true);
        ropeGO.transform.position = transform.position;

        SpriteRenderer sr = ropeGO.AddComponent<SpriteRenderer>();
        // create a white sprite (1x1) with pivot at top so scale.y extends down
        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 1f), 1f);
        sr.sprite = whiteSprite;
        sr.color = Color.white;
        // Ensure sprite uses the default sprite shader and is drawn on top
        sr.material = new Material(Shader.Find("Sprites/Default"));
        try
        {
            sr.sortingLayerName = "Foreground"; // if your project doesn't have this layer, Unity will fall back to Default
        }
        catch { }
        sr.sortingOrder = 100; // draw on top

        // Also create a small visible anchor marker so you can always see where the hook attached
        GameObject anchorVis = new GameObject("GrappleAnchorVis");
        anchorVis.transform.SetParent(transform, true);
        anchorVis.transform.localPosition = Vector3.zero;
        SpriteRenderer aSr = anchorVis.AddComponent<SpriteRenderer>();
        aSr.sprite = whiteSprite;
        aSr.color = Color.red;
        aSr.material = new Material(Shader.Find("Sprites/Default"));
        aSr.sortingOrder = 101;
        // scale anchor marker to a small circle
        anchorVis.transform.localScale = new Vector3(ropeWidth * 4f, ropeWidth * 4f, 1f);

        // start with zero length and grow
        currentRopeLength = 0f;
        ropeGO.transform.localScale = new Vector3(ropeWidth, currentRopeLength, 1f);

        // animate the rope dropping
        StartCoroutine(GrowRopeCoroutine());
    }

    private System.Collections.IEnumerator GrowRopeCoroutine()
    {
        while (currentRopeLength < targetRopeLength)
        {
            float delta = ropeDropSpeed * Time.deltaTime;
            currentRopeLength = Mathf.Min(targetRopeLength, currentRopeLength + delta);
            if (ropeGO != null)
            {
                ropeGO.transform.localScale = new Vector3(ropeWidth, currentRopeLength, 1f);
                // ensure top of sprite stays at anchor position
                ropeGO.transform.position = transform.position;
                // also draw a debug line so rope is visible in Game view (and Scene view)
                Debug.DrawLine(transform.position, transform.position + Vector3.down * currentRopeLength, Color.white, 0f, false);
            }
            yield return null;
        }
    }

    

    private void OnDrawGizmos()
    {
        // Draw anchor position and potential rope reach in the editor for debugging
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.08f);

        float drawLen = (Application.isPlaying) ? currentRopeLength : maxRopeLength;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * drawLen);
    }

    private void Update()
    {
        // Draw a persistent line representing the rope (visible in Game view when Gizmos are enabled)
        if (currentRopeLength > 0f)
        {
            Debug.DrawLine(transform.position, transform.position + Vector3.down * currentRopeLength, Color.cyan);
        }

        if (!active || player == null || inputHandler == null || playerRb == null) return;

        // Can only climb when player is roughly beneath or close horizontally and presses up/down
        Vector2 playerPos = player.position;
        float horizDist = Mathf.Abs(playerPos.x - transform.position.x);
        float vertDist = transform.position.y - playerPos.y; // positive when anchor above player

        if (horizDist <= climbHorizontalTolerance && vertDist > 0f && vertDist > maxClimbHeightOffset)
        {
            // While the player is near the rope, remove gravity effect so they 'stick' to it
            if (!gravityModified && playerRb != null)
            {
                originalPlayerGravity = playerRb.gravityScale;
                playerRb.gravityScale = 0f;
                gravityModified = true;
            }

            // If player pushes up, move them upward along the rope
            if (inputHandler.MovementInput.y > 0f)
            {
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, climbSpeed);
            }
            else if (inputHandler.MovementInput.y < 0f)
            {
                // allow descending
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -climbSpeed);
            }
            else
            {
                // hold position on rope (no vertical velocity)
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
            }
        }
        else
        {
            // restore gravity when player is no longer attached/close
            if (gravityModified && playerRb != null)
            {
                playerRb.gravityScale = originalPlayerGravity;
                gravityModified = false;
            }
        }
    }

    private void OnDestroy()
    {
        // Restore player gravity if we modified it so the player doesn't remain flying
        if (gravityModified && playerRb != null)
        {
            playerRb.gravityScale = originalPlayerGravity;
            gravityModified = false;
        }

        // Optionally, you can spawn a break effect here
    }
}
