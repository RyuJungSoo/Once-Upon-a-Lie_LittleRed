using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
public sealed class SignZigzagAttack : MonoBehaviour
{
    [Header("Zigzag Movement")]
    [SerializeField, Min(0.1f)]
    private float speedMultiplier = 1.75f;

    [SerializeField, Min(0.05f)]
    private float zigzagInterval = 0.18f;

    [SerializeField, Min(0.01f)]
    private float zigzagStrength = 0.65f;

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

    private float movementStartedTime;
    private float nextAttackTime;
    private float attackAnimationEndTime = -1f;
    private int currentSegmentIndex;

    public int CurrentSegmentIndex =>
        currentSegmentIndex;

    public bool IsMovingZigzag =>
        body != null &&
        body.linearVelocity.sqrMagnitude > 0.0001f;

    public float AttackCooldown
    {
        get
        {
            MonsterContactAttackSettings settings =
                GetContactAttackSettings();

            return settings != null
                ? settings.AttackCooldown
                : 0.75f;
        }
    }

    public float AttackAnimationDuration
    {
        get
        {
            MonsterContactAttackSettings settings =
                GetContactAttackSettings();

            return settings != null
                ? settings.AttackAnimationDuration
                : 0.25f;
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        chase = GetComponent<MonsterChase>();
        knockback = GetComponent<MonsterKnockback>();
        appearance =
            GetComponent<MonsterSanityAppearance>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (appearance != null)
        {
            appearance.SetAutomaticMovementDetection(
                false
            );
        }

        ResolveTarget();
        ResolvePlayerMental();
    }

    private void OnEnable()
    {
        movementStartedTime = Time.time;
        nextAttackTime = 0f;
        attackAnimationEndTime = -1f;
        currentSegmentIndex = 0;

        SetChaseEnabled(false);
    }

    private void OnDisable()
    {
        StopMovement();
        SetChaseEnabled(true);
    }

    private void Update()
    {
        if (appearance == null ||
            attackAnimationEndTime < 0f ||
            Time.time < attackAnimationEndTime)
        {
            return;
        }

        attackAnimationEndTime = -1f;
        appearance.RestoreMovementMotionState();
    }

    private void FixedUpdate()
    {
        SetChaseEnabled(false);

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
            StopMovement();
            return;
        }

        if (target == null)
        {
            ResolveTarget();
        }

        if (target == null)
        {
            StopMovement();
            return;
        }

        float elapsedTime =
            Time.time - movementStartedTime;
        currentSegmentIndex = Mathf.FloorToInt(
            elapsedTime / zigzagInterval
        );

        ApplyZigzagMovement(currentSegmentIndex);
    }

    private void ApplyZigzagMovement(
        int segmentIndex
    )
    {
        if (body == null ||
            monsterHealth == null ||
            target == null)
        {
            return;
        }

        Vector2 forwardDirection =
            (Vector2)target.position - body.position;

        if (forwardDirection.sqrMagnitude <=
            0.0001f)
        {
            StopMovement();
            return;
        }

        forwardDirection.Normalize();

        Vector2 perpendicularDirection =
            new Vector2(
                -forwardDirection.y,
                forwardDirection.x
            );

        float lateralSign =
            segmentIndex % 2 == 0
                ? 1f
                : -1f;

        Vector2 movementDirection =
            (
                forwardDirection +
                perpendicularDirection *
                lateralSign *
                zigzagStrength
            ).normalized;

        body.linearVelocity =
            movementDirection *
            monsterHealth.MoveSpeed *
            speedMultiplier;

        if (appearance != null)
        {
            appearance.SetMoving(true);
        }

        UpdateFacing(forwardDirection);
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
        if (!collision.gameObject.CompareTag("Player") ||
            Time.time < nextAttackTime ||
            monsterHealth == null ||
            monsterHealth.IsDead)
        {
            return;
        }

        if (GameManager.HasInstance &&
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        ResolvePlayerMental();

        if (playerMental == null ||
            playerMental.IsDepleted)
        {
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

        nextAttackTime = Time.time + AttackCooldown;
        PlayAttackAnimation();
    }

    private void PlayAttackAnimation()
    {
        if (appearance == null)
        {
            return;
        }

        appearance.SetMotionState(
            MonsterSanityAppearance
                .MonsterMotionState.Attack
        );

        attackAnimationEndTime =
            Time.time + AttackAnimationDuration;
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

    private MonsterContactAttackSettings
        GetContactAttackSettings()
    {
        if (monsterHealth == null)
        {
            monsterHealth = GetComponent<MonsterHealth>();
        }

        MonsterStats stats = monsterHealth != null
            ? monsterHealth.Stats
            : null;

        return stats != null
            ? stats.ContactAttack
            : null;
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

        if (appearance != null)
        {
            appearance.SetMoving(false);
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
        speedMultiplier =
            Mathf.Max(0.1f, speedMultiplier);
        zigzagInterval =
            Mathf.Max(0.05f, zigzagInterval);
        zigzagStrength =
            Mathf.Max(0.01f, zigzagStrength);
    }
}
