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
    [SerializeField] private float downwardAttackBounce = 10f;

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

    void Start()
    {
        attackZone = GameObject.FindGameObjectWithTag("AttackZone");
        startPos = attackZone.transform.localPosition;
        player_Controller = GetComponent<Player_Controller>();
        moveSpeed = player_Controller.MoveSpeed;
        jumpingPower = player_Controller.JumpForce;
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleDirection();
        HandleJump();
    }

    private bool IsGrounded()
    {
        LayerMask layers = groundLayer | platformLayer;
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, layers);
    }

    private void HandleMovement()
    {
        // Calculate platform movement
        Vector2 platformMovement = Vector2.zero;
        if (currentPlatform != null && IsGrounded())
        {
            Vector2 currentPlatformPos = currentPlatform.transform.position;
            platformMovement = currentPlatformPos - lastPlatformPosition;
            lastPlatformPosition = currentPlatformPos;
        }

        // Apply player input movement + platform movement
        float targetX = player_InputHandler.MovementInput.x * moveSpeed + platformMovement.x / Time.fixedDeltaTime;
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
            isDownwardAttacking = false ;
        }
    }

    private void HandleJump()
    {
        if (player_InputHandler.JumpTriggered && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);

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