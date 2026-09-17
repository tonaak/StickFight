using System.Collections;
using UnityEngine;

namespace StickFight.Enemies
{
    /// <summary>
    /// Basic enemy driven by a 4-state machine (Idle, Chase, Attack, Hurt).
    /// Moves the Rigidbody2D in FixedUpdate, per project convention (see CLAUDE.md).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Enemy : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Chase,
            Attack,
            Hurt
        }

        [Header("Chase")]
        [SerializeField] private float detectionRange = 6f;
        [SerializeField] private float moveSpeed = 3f;

        [Header("Attack")]
        [SerializeField] private float attackTriggerRange = 1.5f;
        [SerializeField] private float attackRadius = 0.5f;
        [SerializeField] private float attackOffsetX = 0.6f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float attackKnockbackForce = 8f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private Color attackFlashColor = Color.yellow;
        [SerializeField] private float attackFlashDuration = 0.2f;

        [Header("Hurt")]
        [SerializeField] private float hurtDuration = 0.5f;

        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Color defaultSpriteColor;
        private Coroutine flashCoroutine;
        private Transform playerTransform;

        private State currentState = State.Idle;
        private float hurtTimer;
        private float attackCooldownTimer;
        private bool isFacingRight = true;
        private float currentHealth;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            defaultSpriteColor = spriteRenderer.color;
            currentHealth = maxHealth;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        private void FixedUpdate()
        {
            switch (currentState)
            {
                case State.Idle:
                    TickIdle();
                    break;
                case State.Chase:
                    TickChase();
                    break;
                case State.Attack:
                    TickAttack();
                    break;
                case State.Hurt:
                    TickHurt();
                    break;
            }
        }

        // Stands still while watching for the player to enter detection range.
        private void TickIdle()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (DistanceToPlayer() < detectionRange)
            {
                currentState = State.Chase;
            }
        }

        // Moves toward the player and keeps the sprite facing the direction of travel.
        private void TickChase()
        {
            if (playerTransform == null)
            {
                currentState = State.Idle;
                return;
            }

            if (DistanceToPlayer() >= detectionRange)
            {
                currentState = State.Idle;
                return;
            }

            if (DistanceToPlayer() < attackTriggerRange)
            {
                EnterAttack();
                return;
            }

            float direction = Mathf.Sign(playerTransform.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            Flip(direction > 0f);
        }

        // Stays put through the swing and its cooldown, then either swings again
        // or hands control back to Chase/Idle based on distance.
        private void TickAttack()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            attackCooldownTimer -= Time.fixedDeltaTime;
            if (attackCooldownTimer <= 0f)
            {
                if (DistanceToPlayer() < attackTriggerRange)
                {
                    PerformAttack();
                }
                else
                {
                    currentState = DistanceToPlayer() < detectionRange ? State.Chase : State.Idle;
                }
            }
        }

        // Waits out the stagger from a hit, then re-evaluates whether to chase or idle.
        private void TickHurt()
        {
            hurtTimer -= Time.fixedDeltaTime;
            if (hurtTimer <= 0f)
            {
                currentState = DistanceToPlayer() < detectionRange ? State.Chase : State.Idle;
            }
        }

        private void EnterAttack()
        {
            currentState = State.Attack;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            PerformAttack();
        }

        // Scans a circle in front of the enemy, mirrored to whichever way it is
        // currently facing, for anything on the Player layer.
        private void PerformAttack()
        {
            if (playerTransform != null)
            {
                Flip(playerTransform.position.x - transform.position.x > 0f);
            }

            Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
            Vector2 attackPosition = (Vector2)transform.position + direction * attackOffsetX;

            Collider2D hit = Physics2D.OverlapCircle(attackPosition, attackRadius, playerLayer);

            PlayFlash(attackFlashColor, attackFlashDuration);

            if (hit != null)
            {
                var player = hit.GetComponent<StickFight.Player.PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(direction * attackKnockbackForce, attackDamage);
                }
            }

            attackCooldownTimer = attackCooldown;
        }

        private void Flip(bool faceRight)
        {
            isFacingRight = faceRight;
            spriteRenderer.flipX = !isFacingRight;
        }

        private float DistanceToPlayer()
        {
            if (playerTransform == null)
            {
                return Mathf.Infinity;
            }
            return Vector2.Distance(transform.position, playerTransform.position);
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

        // knockback is the full force vector (direction * magnitude), chosen by the attacker.
        public void TakeDamage(Vector2 knockback, float damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                Die();
                return;
            }

            currentState = State.Hurt;
            hurtTimer = hurtDuration;

            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockback, ForceMode2D.Impulse);
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}
