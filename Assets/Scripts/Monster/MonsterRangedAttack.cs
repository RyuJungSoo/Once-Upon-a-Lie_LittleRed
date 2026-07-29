using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
public sealed class MonsterRangedAttack : MonoBehaviour
{
    private MonsterHealth monsterHealth;
    private MonsterChase chase;
    private MonsterSanityAppearance appearance;
    private SpriteRenderer spriteRenderer;

    private float nextAttackTime;
    private float attackAnimationEndTime = -1f;
    private bool isHoldingAttackRange;
    private bool ownsChasePause;

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

        Fire(direction, settings);
    }

    private void Fire(
        Vector2 direction,
        MonsterRangedAttackSettings settings
    )
    {
        Vector2 normalizedDirection =
            direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
        float horizontalDirection =
            normalizedDirection.x < 0f ? -1f : 1f;
        Vector2 spawnPosition =
            (Vector2)transform.position +
            new Vector2(
                settings.ProjectileSpawnOffset.x *
                horizontalDirection,
                settings.ProjectileSpawnOffset.y
            );

        MonsterProjectile projectile = Instantiate(
            settings.ProjectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        projectile.Launch(
            normalizedDirection,
            settings.ProjectileSpeed,
            settings.ProjectileDamage,
            settings.ProjectileLifetime
        );

        nextAttackTime =
            Time.time + settings.AttackCooldown;
        PlayAttackAnimation(
            settings.AttackAnimationDuration
        );
    }

    private void SetHoldingAttackRange(bool shouldHold)
    {
        isHoldingAttackRange = shouldHold;

        if (shouldHold)
        {
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
        if (!ownsChasePause)
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
