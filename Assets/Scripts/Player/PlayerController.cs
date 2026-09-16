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

        [Header("Jump")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private float groundCheckDistance = 0.1f;

        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;

        // Raw input read on the main thread, consumed in FixedUpdate.
        private float moveInput;
        private bool jumpRequested;
        private bool isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
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

        // Moves the character via Rigidbody2D velocity, independent of framerate.
        private void Move()
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        private void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // Checks a thin box just below the character's feet, derived from its own
        // collider bounds, so it works without needing a separate ground-check transform.
        private bool CheckGrounded()
        {
            Bounds bounds = boxCollider.bounds;
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - groundCheckDistance * 0.5f);
            Vector2 size = new Vector2(bounds.size.x * 0.9f, groundCheckDistance);
            return Physics2D.OverlapBox(origin, size, 0f, groundLayer) != null;
        }
    }
}
