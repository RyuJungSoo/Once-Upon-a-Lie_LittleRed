using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
public sealed class PigChargeAttack : MonoBehaviour
{
    public enum ChargeState
    {
        Chasing,
        Aiming,
        Charging,
        Recovering
    }

    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float chargeRange = 4f;

    [Header("Aiming")]
    [SerializeField, Min(0f)]
    private float aimDuration = 0.65f;

    [Header("Charge")]
    [SerializeField, Min(0.1f)]
    private float chargeSpeed = 8f;

    [SerializeField, Min(0.05f)]
    private float chargeDuration = 0.75f;

    [Header("Recovery")]
    [SerializeField, Min(0f)]
    private float recoveryDuration = 0.65f;

    [SerializeField, Min(0f)]
    private float chargeCooldown = 1.5f;

    [Header("Target")]
    [SerializeField]
    private Transform target;

    private Rigidbody2D body;
    private MonsterHealth monsterHealth;
    private MonsterChase chase;
    private MonsterKnockback knockback;
    private MonsterSanityAppearance appearance;
    private SpriteRenderer spriteRenderer;
    private PlayerMental playerMental;

    private ChargeState currentState;
    private Vector2 chargeDirection;
    private float stateEndTime;
    private float nextChargeTime;
    private bool hasDamagedThisCharge;

    public ChargeState CurrentState => currentState;
    public Vector2 ChargeDirection => chargeDirection;
    public bool IsCharging =>
        currentState == ChargeState.Charging;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        chase = GetComponent<MonsterChase>();
        knockback = GetComponent<MonsterKnockback>();
        appearance =
            GetComponent<MonsterSanityAppearance>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        ResolveTarget();
        ResolvePlayerMental();
    }

    private void OnEnable()
    {
        currentState = ChargeState.Chasing;
        chargeDirection = Vector2.zero;
        stateEndTime = 0f;
        nextChargeTime = 0f;
        hasDamagedThisCharge = false;

        SetChaseEnabled(true);
    }

    private void OnDisable()
    {
        StopMovement();
    }

    private void FixedUpdate()
    {
        if (monsterHealth == null ||
            monsterHealth.IsDead)
        {
            StopMovement();
            return;
        }

        if (GameManager.HasInstance &&
            !GameManager.Instance.IsPlaying)
        {
            StopMovement();
            return;
        }

        if (knockback != null &&
            knockback.IsActive)
        {
            if (currentState != ChargeState.Chasing)
            {
                BeginRecovery();
            }

            StopMovement();
            return;
        }

        if (target == null)
        {
            ResolveTarget();
        }

        switch (currentState)
        {
            case ChargeState.Chasing:
                UpdateChasing();
                break;

            case ChargeState.Aiming:
                UpdateAiming();
                break;

            case ChargeState.Charging:
                UpdateCharging();
                break;

            case ChargeState.Recovering:
                UpdateRecovery();
                break;
        }
    }

    private void UpdateChasing()
    {
        SetChaseEnabled(true);

        if (target == null ||
            Time.time < nextChargeTime)
        {
            return;
        }

        Vector2 offset =
            (Vector2)target.position - body.position;

        if (offset.sqrMagnitude <=
            chargeRange * chargeRange)
        {
            BeginAiming();
        }
    }

    private void UpdateAiming()
    {
        StopMovement();
        UpdateAimDirection();

        if (Time.time >= stateEndTime)
        {
            BeginCharge();
        }
    }

    private void UpdateCharging()
    {
        ApplyChargeMovement();

        if (Time.time >= stateEndTime)
        {
            BeginRecovery();
        }
    }

    private void UpdateRecovery()
    {
        StopMovement();

        if (Time.time < stateEndTime)
        {
            return;
        }

        currentState = ChargeState.Chasing;
        SetChaseEnabled(true);

        if (appearance != null)
        {
            appearance.RestoreMovementMotionState();
        }
    }

    private void BeginAiming()
    {
        currentState = ChargeState.Aiming;
        stateEndTime = Time.time + aimDuration;

        SetChaseEnabled(false);
        StopMovement();
        UpdateAimDirection();

        if (appearance != null)
        {
            appearance.SetMotionState(
                MonsterSanityAppearance
                    .MonsterMotionState.Idle
            );
        }
    }

    private void UpdateAimDirection()
    {
        if (target == null)
        {
            return;
        }

        Vector2 direction =
            (Vector2)target.position - body.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        chargeDirection = direction.normalized;
        UpdateFacing(chargeDirection);
    }

    private void BeginCharge()
    {
        if (chargeDirection.sqrMagnitude <= 0.0001f)
        {
            BeginRecovery();
            return;
        }

        currentState = ChargeState.Charging;
        stateEndTime = Time.time + chargeDuration;
        hasDamagedThisCharge = false;

        if (appearance != null)
        {
            appearance.SetMotionState(
                MonsterSanityAppearance
                    .MonsterMotionState.Attack
            );
        }

        ApplyChargeMovement();
    }

    private void ApplyChargeMovement()
    {
        if (body == null)
        {
            return;
        }

        body.linearVelocity =
            chargeDirection * chargeSpeed;
    }

    private void BeginRecovery()
    {
        if (currentState == ChargeState.Recovering)
        {
            return;
        }

        currentState = ChargeState.Recovering;
        stateEndTime = Time.time + recoveryDuration;
        nextChargeTime =
            stateEndTime + chargeCooldown;

        StopMovement();

        if (appearance != null)
        {
            appearance.SetMotionState(
                MonsterSanityAppearance
                    .MonsterMotionState.Idle
            );
        }
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        TryHitPlayer(collision);
    }

    private void OnCollisionStay2D(
        Collision2D collision
    )
    {
        TryHitPlayer(collision);
    }

    private void TryHitPlayer(Collision2D collision)
    {
        if (!IsCharging ||
            hasDamagedThisCharge ||
            !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        ResolvePlayerMental();

        if (playerMental == null ||
            playerMental.IsDepleted)
        {
            BeginRecovery();
            return;
        }

        playerMental.TakeMentalDamage(
            monsterHealth.Damage
        );

        PlayerMovement playerMovement =
            collision.gameObject
                .GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ApplyKnockback(
                transform.position
            );
        }

        hasDamagedThisCharge = true;
        BeginRecovery();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target = player.transform;
        }
    }

    private void ResolvePlayerMental()
    {
        if (playerMental != null)
        {
            return;
        }

        if (GameManager.HasInstance)
        {
            playerMental = GameManager.Instance
                .GetComponent<PlayerMental>();
        }

        if (playerMental == null)
        {
            playerMental =
                FindFirstObjectByType<PlayerMental>();
        }
    }

    private void SetChaseEnabled(bool shouldEnable)
    {
        if (chase != null &&
            chase.enabled != shouldEnable)
        {
            chase.enabled = shouldEnable;
        }
    }

    private void StopMovement()
    {
        if (body != null &&
            body.linearVelocity != Vector2.zero)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (spriteRenderer == null ||
            Mathf.Abs(direction.x) <= 0.001f)
        {
            return;
        }

        spriteRenderer.flipX = direction.x < 0f;
    }

    private void OnValidate()
    {
        chargeRange = Mathf.Max(0.1f, chargeRange);
        aimDuration = Mathf.Max(0f, aimDuration);
        chargeSpeed = Mathf.Max(0.1f, chargeSpeed);
        chargeDuration =
            Mathf.Max(0.05f, chargeDuration);
        recoveryDuration =
            Mathf.Max(0f, recoveryDuration);
        chargeCooldown =
            Mathf.Max(0f, chargeCooldown);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            new Color(1f, 0.45f, 0.1f, 0.75f);
        Gizmos.DrawWireSphere(
            transform.position,
            chargeRange
        );
    }
}
