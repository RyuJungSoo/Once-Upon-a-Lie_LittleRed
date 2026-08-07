using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class MothFanAttack : MonoBehaviour
{
    [Header("Fan Pattern")]
    [SerializeField, Min(2)]
    private int projectileCount = 5;

    [SerializeField, Range(0f, 180f)]
    private float spreadAngle = 60f;

    private MonsterHealth monsterHealth;
    private MonsterChase chase;
    private MonsterSanityAppearance appearance;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;

    private float nextAttackTime;
    private float attackAnimationEndTime = -1f;
    private bool isHoldingAttackRange;
    private bool ownsChasePause;
    private bool ownsPositionFreeze;
    private RigidbodyConstraints2D positionConstraintsBeforeHold;

    public int ProjectileCount => projectileCount;
    public float SpreadAngle => spreadAngle;

    public MonsterRangedAttackSettings Settings
    {
        get
        {
            if (monsterHealth == null)
            {
                monsterHealth = GetComponent<MonsterHealth>();
            }

            MonsterStats stats = monsterHealth != null
                ? monsterHealth.Stats
                : null;

            return stats != null
                ? stats.RangedAttack
                : null;
        }
    }

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        chase = GetComponent<MonsterChase>();
        appearance = GetComponent<MonsterSanityAppearance>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
    }

    private void OnValidate()
    {
        projectileCount = Mathf.Max(2, projectileCount);
        spreadAngle = Mathf.Clamp(spreadAngle, 0f, 180f);
    }

    private void OnEnable()
    {
        nextAttackTime = Time.time;
        attackAnimationEndTime = -1f;
        isHoldingAttackRange = false;
        ownsChasePause = false;
        ownsPositionFreeze = false;
    }

    private void OnDisable()
    {
        ReleaseChase();
        RestoreMovementAnimation();
    }

    private void Update()
    {
        RestoreMovementAnimationWhenReady();

        if (monsterHealth.IsDead ||
            (GameManager.HasInstance &&
             !GameManager.Instance.IsPlaying))
        {
            SetHoldingAttackRange(false);
            return;
        }

        Transform target = chase.Target;
        MonsterRangedAttackSettings settings = Settings;

        if (target == null || settings == null)
        {
            SetHoldingAttackRange(false);
            return;
        }

        Vector2 direction =
            (Vector2)target.position -
            (Vector2)transform.position;
        float activeRange =
            isHoldingAttackRange
                ? settings.AttackRange +
                  settings.ResumeRangePadding
                : settings.AttackRange;

        if (direction.sqrMagnitude >
            activeRange * activeRange)
        {
            SetHoldingAttackRange(false);
            return;
        }

        SetHoldingAttackRange(true);
        FaceTarget(direction);

        if (settings.ProjectilePrefab == null ||
            Time.time < nextAttackTime)
        {
            return;
        }

        FireFan(direction, settings);
    }

    private void FireFan(
        Vector2 direction,
        MonsterRangedAttackSettings settings
    )
    {
        Vector2 centerDirection =
            direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
        float horizontalDirection =
            centerDirection.x < 0f ? -1f : 1f;
        Vector2 spawnPosition =
            (Vector2)transform.position +
            new Vector2(
                settings.ProjectileSpawnOffset.x *
                horizontalDirection,
                settings.ProjectileSpawnOffset.y
            );

        for (int index = 0; index < projectileCount; index++)
        {
            Vector2 shotDirection =
                CalculateShotDirection(
                    centerDirection,
                    index,
                    projectileCount,
                    spreadAngle
                );
            MonsterProjectile projectile = Instantiate(
                settings.ProjectilePrefab,
                spawnPosition,
                Quaternion.identity
            );

            projectile.Launch(
                shotDirection,
                settings.ProjectileSpeed,
                settings.ProjectileDamage,
                settings.ProjectileLifetime
            );
        }

        nextAttackTime =
            Time.time + settings.AttackCooldown;
        PlayAttackAnimation(
            settings.AttackAnimationDuration
        );
    }

    private static Vector2 CalculateShotDirection(
        Vector2 centerDirection,
        int shotIndex,
        int shotCount,
        float totalSpreadAngle
    )
    {
        if (shotCount <= 1)
        {
            return centerDirection.normalized;
        }

        float angleStep =
            totalSpreadAngle / (shotCount - 1);
        float angle =
            -totalSpreadAngle * 0.5f +
            angleStep * shotIndex;

        return Quaternion.Euler(0f, 0f, angle) *
            centerDirection.normalized;
    }

    private void SetHoldingAttackRange(bool shouldHold)
    {
        isHoldingAttackRange = shouldHold;

        if (shouldHold)
        {
            FreezePosition();

            if (chase.enabled)
            {
                chase.enabled = false;
                ownsChasePause = true;
            }

            return;
        }

        ReleaseChase();
    }

    private void ReleaseChase()
    {
        ReleasePosition();

        if (ownsChasePause)
        {
            chase.enabled = true;
            ownsChasePause = false;
        }
    }

    private void FreezePosition()
    {
        if (ownsPositionFreeze)
        {
            return;
        }

        positionConstraintsBeforeHold =
            body.constraints &
            RigidbodyConstraints2D.FreezePosition;
        body.linearVelocity = Vector2.zero;
        body.constraints |=
            RigidbodyConstraints2D.FreezePosition;
        ownsPositionFreeze = true;
    }

    private void ReleasePosition()
    {
        if (!ownsPositionFreeze)
        {
            return;
        }

        body.constraints =
            (body.constraints &
             ~RigidbodyConstraints2D.FreezePosition) |
            positionConstraintsBeforeHold;
        ownsPositionFreeze = false;
    }

    private void FaceTarget(Vector2 direction)
    {
        if (spriteRenderer == null ||
            Mathf.Abs(direction.x) <= 0.001f)
        {
            return;
        }

        spriteRenderer.flipX = direction.x < 0f;
    }

    private void PlayAttackAnimation(
        float animationDuration
    )
    {
        if (appearance == null)
        {
            return;
        }

        appearance.SetMotionState(
            MonsterSanityAppearance.MonsterMotionState.Attack
        );
        attackAnimationEndTime =
            Time.time + animationDuration;
    }

    private void RestoreMovementAnimationWhenReady()
    {
        if (attackAnimationEndTime < 0f ||
            Time.time < attackAnimationEndTime)
        {
            return;
        }

        RestoreMovementAnimation();
    }

    private void RestoreMovementAnimation()
    {
        if (attackAnimationEndTime < 0f)
        {
            return;
        }

        attackAnimationEndTime = -1f;
        appearance?.RestoreMovementMotionState();
    }
}
