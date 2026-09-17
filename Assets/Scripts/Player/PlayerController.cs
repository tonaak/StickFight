using UnityEngine;
using UnityEngine.InputSystem;

namespace StickFight.Player
{
    /// <summary>
    /// Handles horizontal movement and jumping for the player character.
    /// Uses the New Input System and moves the Rigidbody2D in FixedUpdate,
    /// per project convention (see CLAUDE.md).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 80f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;

        private Rigidbody2D rb;

        // Raw input read on the main thread, consumed in FixedUpdate.
        private float moveInput;
        private bool jumpRequested;
        private bool isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            ReadInput();
        }

        private void FixedUpdate()
        {
            isGrounded = CheckGrounded();

            Move();

            if (jumpRequested && isGrounded)
            {
                Jump();
            }
            jumpRequested = false;
        }

        // Reads player input via the New Input System (no legacy Input.GetKey).
        private void ReadInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            moveInput = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                moveInput -= 1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                moveInput += 1f;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpRequested = true;
            }
        }

        // Accelerates toward the target speed and decelerates sharply to a stop
        // when input is released, avoiding an "ice skating" feel.
        private void Move()
        {
            float targetSpeed = moveInput * moveSpeed;
            float rate = Mathf.Abs(moveInput) > 0.01f ? acceleration : deceleration;
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        private void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // Checks a small circle at the GroundCheck transform (placed at the character's
        // feet) against the dedicated Ground layer only, so the player's own collider
        // can never register as ground.
        private bool CheckGrounded()
        {
            if (groundCheck == null)
            {
                return false;
            }
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
        }
    }
}
