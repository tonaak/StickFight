using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace StickFight.Player
{
    /// <summary>
    /// Handles horizontal movement, jumping, and melee attacks for the player character.
    /// Uses the New Input System and moves the Rigidbody2D in FixedUpdate,
    /// per project convention (see CLAUDE.md).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
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

        [Header("Melee Attack")]
        [SerializeField] private float attackRange = 0.5f;
        [SerializeField] private float attackOffsetX = 0.6f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private Color hitFlashColor = Color.red;
        [SerializeField] private float hitFlashDuration = 0.1f;

        [Header("Combo")]
        [SerializeField] private float comboWindow = 0.5f;
        [SerializeField] private float comboCooldown = 0.2f;
        [SerializeField] private float comboDamage = 35f;
        [SerializeField] private float comboKnockbackForce = 5f;
        [SerializeField] private float finisherCooldown = 0.4f;
        [SerializeField] private float finisherDamage = 60f;
        [SerializeField] private float finisherKnockbackMultiplier = 3f;
        [SerializeField] private Color finisherFlashColor = new Color(1f, 0.5f, 0f);

        [Header("Hit Stop")]
        [SerializeField] private float hitStopTimeScale = 0.1f;
        [SerializeField] private float hitStopDuration = 0.05f;

        [Header("Camera Shake")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private float hitShakeDuration = 0.04f;
        [SerializeField] private float hitShakeMagnitude = 0.05f;
        [SerializeField] private float finisherShakeDuration = 0.08f;
        [SerializeField] private float finisherShakeMagnitude = 0.15f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Dash")]
        [SerializeField] private float dashForce = 20f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1f;

        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float knockbackForce = 8f;
        [SerializeField] private Color damageFlashColor = Color.blue;
        [SerializeField] private float damageFlashDuration = 0.2f;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Color defaultSpriteColor;
        private Coroutine flashCoroutine;
        private Coroutine hitStopCoroutine;

        // Raw input read on the main thread, consumed in FixedUpdate.
        private float moveInput;
        private bool jumpRequested;
        private bool attackRequested;
        private bool dashRequested;
        private bool isGrounded;
        private bool isFacingRight = true;
        private float nextAttackTime;
        private float currentHealth;

        private int comboStep;
        private float lastAttackTime;

        private bool isDashing;
        private float dashTimer;
        private float nextDashTime;
        private float defaultGravityScale;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            defaultSpriteColor = spriteRenderer.color;
            defaultGravityScale = rb.gravityScale;
            currentHealth = maxHealth;
        }

        // Guards against a stuck slow-motion timescale or zero-gravity state if the
        // object is disabled mid hit-stop or mid-dash.
        private void OnDisable()
        {
            if (hitStopCoroutine != null)
            {
                Time.timeScale = 1f;
            }
            if (isDashing)
            {
                rb.gravityScale = defaultGravityScale;
            }
        }

        private void Update()
        {
            ReadInput();
        }

        private void FixedUpdate()
        {
            isGrounded = CheckGrounded();

            if (dashRequested && !isDashing && Time.time >= nextDashTime)
            {
                StartDash();
            }
            dashRequested = false;

            if (isDashing)
            {
                TickDash();
            }
            else
            {
                Move();
            }

            if (jumpRequested && isGrounded && !isDashing)
            {
                Jump();
            }
            jumpRequested = false;

            if (attackRequested && Time.time >= nextAttackTime)
            {
                Attack();
            }
            attackRequested = false;
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

            if (keyboard.jKey.wasPressedThisFrame)
            {
                attackRequested = true;
            }

            if (keyboard.kKey.wasPressedThisFrame || keyboard.leftShiftKey.wasPressedThisFrame)
            {
                dashRequested = true;
            }
        }

        // Accelerates toward the target speed and decelerates sharply to a stop
        // when input is released, avoiding an "ice skating" feel.
        private void Move()
        {
            if (moveInput > 0.01f)
            {
                isFacingRight = true;
            }
            else if (moveInput < -0.01f)
            {
                isFacingRight = false;
            }
            spriteRenderer.flipX = !isFacingRight;

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

        // Launches the player hard toward the direction they're facing. Gravity is
        // disabled for the dash so the burst of speed isn't immediately fighting it.
        private void StartDash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            nextDashTime = Time.time + dashCooldown;

            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;

            Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
            rb.AddForce(direction * dashForce, ForceMode2D.Impulse);
        }

        private void TickDash()
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }

        private void EndDash()
        {
            isDashing = false;
            rb.gravityScale = defaultGravityScale;
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

        // Scans a circle in front of the character, mirrored to whichever way it is
        // currently facing, for anything on the Enemy layer. A 3-hit combo (Jab, Cross,
        // Kick) advances as long as each press lands within comboWindow of the last;
        // otherwise it resets to the first hit.
        private void Attack()
        {
            if (Time.time - lastAttackTime > comboWindow)
            {
                comboStep = 0;
            }

            bool isFinisher = comboStep == 2;

            Vector2 facingDirection = isFacingRight ? Vector2.right : Vector2.left;
            Vector2 attackPosition = (Vector2)transform.position + facingDirection * attackOffsetX;

            Collider2D hit = Physics2D.OverlapCircle(attackPosition, attackRange, enemyLayer);

            PlayFlash(isFinisher ? finisherFlashColor : hitFlashColor, hitFlashDuration);

            if (animator != null)
            {
                animator.SetTrigger(comboStep == 0 ? "Attack1" : comboStep == 1 ? "Attack2" : "Attack3");
            }

            if (hit != null)
            {
                Debug.Log("Hit Enemy!");
                TriggerHitStop();

                if (cameraFollow != null)
                {
                    if (isFinisher)
                    {
                        cameraFollow.Shake(finisherShakeDuration, finisherShakeMagnitude);
                    }
                    else
                    {
                        cameraFollow.Shake(hitShakeDuration, hitShakeMagnitude);
                    }
                }

                var enemy = hit.GetComponent<StickFight.Enemies.Enemy>();
                if (enemy != null)
                {
                    float damage = isFinisher ? finisherDamage : comboDamage;
                    float knockback = isFinisher ? comboKnockbackForce * finisherKnockbackMultiplier : comboKnockbackForce;
                    enemy.TakeDamage(facingDirection * knockback, damage);
                }
            }

            lastAttackTime = Time.time;
            nextAttackTime = Time.time + (isFinisher ? finisherCooldown : comboCooldown);
            comboStep = (comboStep + 1) % 3;
        }

        // Briefly tints the sprite the given color as feedback, restoring the original after.
        private void PlayFlash(Color color, float duration)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashColor(color, duration));
        }

        private IEnumerator FlashColor(Color color, float duration)
        {
            spriteRenderer.color = color;
            yield return new WaitForSeconds(duration);
            spriteRenderer.color = defaultSpriteColor;
            flashCoroutine = null;
        }

        // Called by enemies (or other damage sources) on a successful hit.
        public void TakeDamage(Vector2 knockback, float damage)
        {
            currentHealth -= damage;
            rb.AddForce(knockback.normalized * knockbackForce, ForceMode2D.Impulse);
            PlayFlash(damageFlashColor, damageFlashDuration);
            Debug.Log("Player took damage!");

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("Player Died!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Briefly slows time on a successful hit to sell the weight of the attack.
        // Uses unscaled time so the pause itself has a fixed real-world duration.
        private void TriggerHitStop()
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }
            hitStopCoroutine = StartCoroutine(HitStop());
        }

        private IEnumerator HitStop()
        {
            Time.timeScale = hitStopTimeScale;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1f;
            hitStopCoroutine = null;
        }
    }
}
