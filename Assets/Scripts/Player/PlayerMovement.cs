using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerMovement : MonoBehaviour
{
    private const string MoveActionPath = "PlayerInput/Move";

    private static readonly int FacingParameter = Animator.StringToHash("Facing");
    private static readonly int MovingParameter = Animator.StringToHash("Moving");
    private static readonly int AttackingParameter = Animator.StringToHash("Attacking");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");

    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField, Min(0f)] private float knockbackDistance = 0.7f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.15f;

    private Rigidbody2D body;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private InputAction moveAction;
    private Vector2 moveInput;
    private FacingDirection facingDirection = FacingDirection.Front;
    private float attackAnimationEndTime;
    private Vector2 knockbackDirection;
    private float knockbackTimeRemaining;

    private enum FacingDirection
    {
        Front = 0,
        Back = 1,
        Side = 2
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (inputActions == null)
        {
            Debug.LogError("PlayerInput Input Actions is not assigned to PlayerMovement.", this);
            enabled = false;
            return;
        }

        moveAction = inputActions.FindAction(MoveActionPath, true);
    }

    private void OnEnable()
    {
        if (moveAction == null)
        {
            return;
        }

        moveAction.Enable();
        animator.SetInteger(FacingParameter, (int)facingDirection);
        animator.SetBool(MovingParameter, false);
        animator.SetBool(AttackingParameter, false);
        animator.ResetTrigger(AttackParameter);
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        StopMovement(true);

        if (animator == null)
        {
            return;
        }

        animator.SetBool(AttackingParameter, false);
        animator.ResetTrigger(AttackParameter);
    }

    private void Update()
    {
        if (!CanMove())
        {
            StopMovement(ShouldCancelPendingKnockback());
            return;
        }

        moveInput = moveAction.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        if (!CanMove())
        {
            StopMovement(ShouldCancelPendingKnockback());
            return;
        }

        if (knockbackTimeRemaining > 0f)
        {
            float stepTime = Mathf.Min(Time.fixedDeltaTime, knockbackTimeRemaining);

            float knockbackSpeed = knockbackDistance / knockbackDuration;

            Vector2 nextPosition = body.position + knockbackDirection * knockbackSpeed * stepTime;

            body.MovePosition(nextPosition);
            knockbackTimeRemaining -= Time.fixedDeltaTime;
            return;
        }

        Vector2 movement = moveInput * (moveSpeed * Time.fixedDeltaTime);

        body.MovePosition(body.position + movement);
    }

    private static bool CanMove()
    {
        return !GameManager.HasInstance ||
               GameManager.Instance.IsPlaying;
    }

    private static bool ShouldCancelPendingKnockback()
    {
        return !GameManager.HasInstance ||
               !GameManager.Instance.IsPaused;
    }

    private void StopMovement(bool cancelPendingKnockback)
    {
        moveInput = Vector2.zero;

        if (cancelPendingKnockback)
        {
            knockbackTimeRemaining = 0f;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        if (animator != null)
        {
            animator.SetBool(MovingParameter, false);
        }
    }

    private void UpdateAnimatorParameters()
    {
        bool isAttacking = animator.GetBool(AttackingParameter);

        if (isAttacking && Time.time >= attackAnimationEndTime)
        {
            isAttacking = false;
            animator.SetBool(AttackingParameter, false);
        }

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        if (!isAttacking && isMoving)
        {
            UpdateFacingDirection(moveInput);
        }

        animator.SetInteger(FacingParameter, (int)facingDirection);
        animator.SetBool(MovingParameter, isMoving);
    }

    private void UpdateFacingDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            facingDirection = FacingDirection.Side;
            spriteRenderer.flipX = direction.x < 0f;
            return;
        }

        facingDirection = direction.y > 0f
            ? FacingDirection.Back
            : FacingDirection.Front;
        spriteRenderer.flipX = false;
    }

    public void PlayAttackAnimation(Vector2 aimDirection, float duration)
    {
        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        UpdateFacingDirection(aimDirection);
        animator.SetInteger(FacingParameter, (int)facingDirection);
        animator.SetBool(AttackingParameter, true);
        animator.ResetTrigger(AttackParameter);
        animator.SetTrigger(AttackParameter);
        attackAnimationEndTime = Time.time + Mathf.Max(0f, duration);
    }

    public void ApplyKnockback(Vector2 attackerPosition)
    {
        Vector2 direction = body.position - attackerPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.down;
        }

        knockbackDirection = direction.normalized;
        knockbackTimeRemaining = knockbackDuration;
    }
}
