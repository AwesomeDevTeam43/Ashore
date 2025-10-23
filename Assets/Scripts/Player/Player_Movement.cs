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
    [SerializeField] private float downwardAttackBounce = 10f;
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimer = 0f;

    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 5f;
    private float velocityXSmoothing;

    private Player_Controller player_Controller;

    private float moveSpeed;
    private float jumpingPower;
    private bool isFacingRight = true;
    private Vector3 vertAtk = new Vector3(0.0f, 1.5f, 0.0f);
    private Vector3 startPos;

    private Map_PlatformMoves currentPlatform;
    private Vector2 lastPlatformPosition;
    private bool isDownwardAttacking = false;

    public bool IsFacingRight => isFacingRight;
    public bool IsFacingLeft => !isFacingRight;

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

        float targetX = player_InputHandler.MovementInput.x * moveSpeed;

        if (platformMovement != Vector2.zero)
            targetX += platformMovement.x / Time.fixedDeltaTime;

        bool hasInput = Mathf.Abs(player_InputHandler.MovementInput.x) > 0.01f;
        float smoothTime = hasInput ? 1f / acceleration : 1f / deceleration;

        float currentX = rb.linearVelocity.x;
        float newX = Mathf.SmoothDamp(currentX, targetX, ref velocityXSmoothing, smoothTime);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
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
            isDownwardAttacking = true;
        }
        else if (player_InputHandler.MovementInput.y > 0f)
        {
            attackZone.transform.localPosition = vertAtk;
            isDownwardAttacking = false;
        }
        else
        {
            attackZone.transform.localPosition = startPos;
            isDownwardAttacking = false;
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
}