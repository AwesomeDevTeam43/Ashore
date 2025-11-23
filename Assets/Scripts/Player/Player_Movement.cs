using NUnit.Framework;
using UnityEngine;

public class Player_Movement : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;
    [SerializeField] private GameObject attackZone;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask platformLayer;
    private int combinedGroundLayers;
    // expose the ground check transform for accurate raycasts
    public Transform GroundCheck => groundCheck;
    // expose combined ground layers so other systems (animator, etc.) can raycast using the same mask
    public LayerMask CombinedGroundLayers => combinedGroundLayers;
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimer = 0f;

    private Player_Controller player_Controller;

    private float moveSpeed;
    private float jumpingPower;
    private bool isFacingRight = true;
    private Vector3 vertAtk = new Vector3(0.0f, 1.5f, 0.0f);
    private Vector3 startPos;

    private Map_PlatformMoves currentPlatform;
    private Vector2 lastPlatformPosition;

    public bool IsFacingRight => isFacingRight;
    public bool IsFacingLeft => !isFacingRight;

    //cheats
    [SerializeField] public Transform place1;
    [SerializeField] public Transform place2;
    [SerializeField] public Transform place3;
    [SerializeField] public Transform place4;

    void Start()
    {
        attackZone = GameObject.FindGameObjectWithTag("AttackZone");
        startPos = attackZone.transform.localPosition;
        player_Controller = GetComponent<Player_Controller>();
        moveSpeed = player_Controller.MoveSpeed;
        jumpingPower = player_Controller.JumpForce;
        combinedGroundLayers = groundLayer | platformLayer;
    }

    void FixedUpdate()
    {
        HandleCheatTeleports();
        HandleMovement();
        HandleDirection();
        HandleJump();
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, combinedGroundLayers);
    }

    private void HandleMovement()
    {
        Vector2 platformMovement = Vector2.zero;
        if (currentPlatform != null && IsGrounded())
        {
            Vector2 currentPlatformPos = currentPlatform.transform.position;
            platformMovement = currentPlatformPos - lastPlatformPosition;
            lastPlatformPosition = currentPlatformPos;
        }

        float targetX = player_InputHandler.MovementInput.x * moveSpeed + (Time.fixedDeltaTime > 0f ? platformMovement.x / Time.fixedDeltaTime : 0f);
        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);
    }

    private void HandleDirection()
    {
        if (isFacingRight && player_InputHandler.MovementInput.x < 0f || !isFacingRight && player_InputHandler.MovementInput.x > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
        if (player_InputHandler.MovementInput.y < 0f && !IsGrounded())
        {
            attackZone.transform.localPosition = -vertAtk;
        }
        else if (player_InputHandler.MovementInput.y > 0f)
        {
            attackZone.transform.localPosition = vertAtk;
        }
        else
        {
            attackZone.transform.localPosition = startPos;
        }
    }

    private void HandleJump()
    {
        if (IsGrounded())
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }

        if (player_InputHandler.JumpTriggered && (IsGrounded() || coyoteTimer > 0f))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
            coyoteTimer = 0f;
        }

        if (!player_InputHandler.JumpTriggered && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("MovingPlatform"))
        {
            currentPlatform = collision.gameObject.GetComponent<Map_PlatformMoves>();
            if (currentPlatform != null)
            {
                lastPlatformPosition = currentPlatform.transform.position;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("MovingPlatform"))
        {
            currentPlatform = null;
        }
    }

    private void HandleCheatTeleports()
    {
        if (!Input.GetKey(KeyCode.Z)) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (place1 == null) return;
            TeleportTo(place1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (place2 == null) return;
            TeleportTo(place2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (place3 == null) return;
            TeleportTo(place3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (place4 == null) return;
            TeleportTo(place4);
        }
    }

    private void TeleportTo(Transform destination)
    {
        if (destination == null) return;

        transform.position = destination.position;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}