using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Vector2 moveInput;
    public Rigidbody2D rb;

    public bool IsMoving { get; private set; }

    void Start()
    {

    }

    void Update()
    {

    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        IsMoving = moveInput != Vector2.zero;
    }
}
