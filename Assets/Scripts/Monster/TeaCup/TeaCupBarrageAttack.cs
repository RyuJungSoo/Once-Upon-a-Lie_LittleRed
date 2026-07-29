using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
public sealed class TeaCupBarrageAttack : MonoBehaviour
{
    [Header("Barrage Pattern")]
    [SerializeField, Min(0.05f)]
    private float fireInterval = 0.18f;

    [SerializeField, Range(0f, 180f)]
    private float sweepAngle = 30f;

    [SerializeField, Min(2)]
    private int sweepLaneCount = 5;

    private Rigidbody2D body;
    private MonsterHealth monsterHealth;
    private MonsterChase chase;
    private MonsterSanityAppearance appearance;
    private SpriteRenderer spriteRenderer;

    private float nextShotTime;
    private float attackAnimationEndTime = -1f;
    private int shotIndex;
    private bool ownsChasePause;

    public float FireInterval => fireInterval;
    public float SweepAngle => sweepAngle;
    public int SweepLaneCount => sweepLaneCount;

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
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        chase = GetComponent<MonsterChase>();
        appearance = GetComponent<MonsterSanityAppearance>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        nextShotTime = Time.time;
        shotIndex = 0;
        HoldPosition();
    }

    private void OnDisable()
    {
        ReleaseChase();
        RestoreMovementAnimation();
    }

    private void OnValidate()
    {
        fireInterval = Mathf.Max(0.05f, fireInterval);
        sweepAngle = Mathf.Clamp(sweepAngle, 0f, 180f);
        sweepLaneCount = Mathf.Max(2, sweepLaneCount);
    }

    private void FixedUpdate()
    {
        HoldPosition();
    }

    private void Update()
    {
        RestoreMovementAnimationWhenReady();
        HoldPosition();

        if (monsterHealth == null ||
            monsterHealth.IsDead ||
            (GameManager.HasInstance &&
             !GameManager.Instance.IsPlaying))
        {
            return;
        }

        Transform target = chase != null
            ? chase.Target
            : null;
        MonsterRangedAttackSettings settings = Settings;

        if (target == null ||
            settings == null ||
            settings.ProjectilePrefab == null)
        {
            return;
        }

        Vector2 direction =
            (Vector2)target.position -
            (Vector2)transform.position;

        if (direction.sqrMagnitude >
            settings.AttackRange * settings.AttackRange)
        {
            return;
        }

        FaceTarget(direction);

        if (Time.time < nextShotTime)
        {
            return;
        }

        Fire(direction, settings);
    }

    private void Fire(
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
        Vector2 shotDirection =
            CalculateSweepDirection(
                centerDirection,
                shotIndex,
                sweepLaneCount,
                sweepAngle
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

        shotIndex++;
        nextShotTime = Time.time + fireInterval;
        PlayAttackAnimation(
            settings.AttackAnimationDuration
        );
    }

    private static Vector2 CalculateSweepDirection(
        Vector2 centerDirection,
        int shotIndex,
        int laneCount,
        float totalSweepAngle
    )
    {
        Vector2 normalizedDirection =
            centerDirection.sqrMagnitude > 0f
                ? centerDirection.normalized
                : Vector2.right;

        if (laneCount <= 1)
        {
            return normalizedDirection;
        }

        int period = (laneCount - 1) * 2;
        int phase = Mathf.Abs(shotIndex) % period;
        int laneIndex =
            phase < laneCount
                ? phase
                : period - phase;
        float laneRatio =
            laneIndex / (laneCount - 1f);
        float angle = Mathf.Lerp(
            -totalSweepAngle * 0.5f,
            totalSweepAngle * 0.5f,
            laneRatio
        );

        return Quaternion.Euler(0f, 0f, angle) *
            normalizedDirection;
    }

    private void HoldPosition()
    {
        if (chase != null && chase.enabled)
        {
            chase.enabled = false;
            ownsChasePause = true;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void ReleaseChase()
    {
        if (!ownsChasePause || chase == null)
        {
            return;
        }

        chase.enabled = true;
        ownsChasePause = false;
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
