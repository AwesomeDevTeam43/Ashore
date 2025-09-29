using NUnit.Framework;
using UnityEngine;

public class Player_Movement : MonoBehaviour
{
    [SerializeField] private Player_InputHandler player_InputHandler;
    [SerializeField] private GameObject attackZone;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private float moveSpeed = 5f;
    private float jumpingPower = 15f;
    private bool isFacingRight = true;
    private Vector3 vertAtk = new Vector3 (0.0f, 1.5f, 0.0f);
    private Vector3 startPos;
    public bool IsFacingRight => isFacingRight;

    void Start()
    {
      attackZone = GameObject.FindGameObjectWithTag("AttackZone");
      startPos = attackZone.transform.localPosition;
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleDirection();
        HandleJump();
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void HandleMovement()
    {
        rb.linearVelocity = new Vector2(player_InputHandler.MovementInput.x * moveSpeed, rb.linearVelocity.y);
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
        if (player_InputHandler.JumpTriggered && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);

        }
        if (!player_InputHandler.JumpTriggered && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }
}