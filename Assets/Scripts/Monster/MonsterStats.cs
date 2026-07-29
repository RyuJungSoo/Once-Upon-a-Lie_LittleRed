using System;
using UnityEngine;

[Serializable]
public sealed class MonsterChaseSettings
{
    [SerializeField, Min(0f)]
    private float stopDistance = 0.1f;

    public float StopDistance => stopDistance;

    internal void Validate()
    {
        stopDistance = Mathf.Max(0f, stopDistance);
    }
}

[Serializable]
public sealed class MonsterContactAttackSettings
{
    [SerializeField, Min(0.05f)]
    private float attackCooldown = 0.75f;

    [SerializeField, Min(0f)]
    private float attackAnimationDuration = 0.25f;

    public float AttackCooldown => attackCooldown;
    public float AttackAnimationDuration =>
        attackAnimationDuration;

    internal void Validate()
    {
        attackCooldown = Mathf.Max(0.05f, attackCooldown);
        attackAnimationDuration = Mathf.Max(
            0f,
            attackAnimationDuration
        );
    }
}

[Serializable]
public sealed class MonsterRangedAttackSettings
{
    [Header("Range")]
    [SerializeField, Min(0f)]
    private float attackRange = 5f;

    [Tooltip("Prevents rapid stop/resume toggling at the range boundary.")]
    [SerializeField, Min(0f)]
    private float resumeRangePadding = 0.5f;

    [Header("Attack")]
    [SerializeField, Min(0.05f)]
    private float attackCooldown = 1.25f;

    [SerializeField, Min(0f)]
    private float attackAnimationDuration = 0.25f;

    [Header("Projectile")]
    [SerializeField]
    private MonsterProjectile projectilePrefab;

    [SerializeField]
    private Vector2 projectileSpawnOffset =
        new Vector2(0f, 0.2f);

    [SerializeField, Min(0f)]
    private float projectileSpeed = 7f;

    [SerializeField, Min(0f)]
    private float projectileDamage = 10f;

    [SerializeField, Min(0.1f)]
    private float projectileLifetime = 4f;

    public float AttackRange => attackRange;
    public float ResumeRangePadding => resumeRangePadding;
    public float AttackCooldown => attackCooldown;
    public float AttackAnimationDuration =>
        attackAnimationDuration;
    public MonsterProjectile ProjectilePrefab =>
        projectilePrefab;
    public Vector2 ProjectileSpawnOffset =>
        projectileSpawnOffset;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileDamage => projectileDamage;
    public float ProjectileLifetime => projectileLifetime;

    internal void Validate()
    {
        attackRange = Mathf.Max(0f, attackRange);
        resumeRangePadding = Mathf.Max(
            0f,
            resumeRangePadding
        );
        attackCooldown = Mathf.Max(0.05f, attackCooldown);
        attackAnimationDuration = Mathf.Max(
            0f,
            attackAnimationDuration
        );
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileDamage = Mathf.Max(0f, projectileDamage);
        projectileLifetime = Mathf.Max(
            0.1f,
            projectileLifetime
        );
    }
}

[CreateAssetMenu(fileName = "MonsterStats", menuName = "Once Upon a Lie/Monster Stats")]
public sealed class MonsterStats : ScriptableObject
{
    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 1;
    [SerializeField, Min(0f)] private float damage = 10f;

    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackDistance = 0.3f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.1f;

    [Header("Hit Feedback")]
    [SerializeField] private Color hitFlashColor =
        new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2f;

    [Header("Chase")]
    [SerializeField]
    private MonsterChaseSettings chase =
        new MonsterChaseSettings();

    [Header("Contact Attack")]
    [SerializeField]
    private MonsterContactAttackSettings contactAttack =
        new MonsterContactAttackSettings();

    [Header("Ranged Attack")]
    [SerializeField]
    private MonsterRangedAttackSettings rangedAttack =
        new MonsterRangedAttackSettings();

    [Header("Experience Crystal Drop")]
    [SerializeField]
    private GameObject experienceCrystalDropPrefab;

    [Header("Recovery Item Drops")]
    [Tooltip("RedBerry prefab dropped by this monster.")]
    [SerializeField]
    private GameObject redBerryDropPrefab;

    [Tooltip("Independent RedBerry drop chance in percent.")]
    [SerializeField, Range(0f, 100f)]
    private float redBerryDropChancePercent = 10f;

    [Tooltip("StarCandy prefab dropped by this monster.")]
    [SerializeField]
    private GameObject starCandyDropPrefab;

    [Tooltip("Independent StarCandy drop chance in percent.")]
    [SerializeField, Range(0f, 100f)]
    private float starCandyDropChancePercent = 20f;

    [Tooltip("Pie prefab dropped by this monster.")]
    [SerializeField]
    private GameObject pieDropPrefab;

    [Tooltip("Independent Pie drop chance in percent.")]
    [SerializeField, Range(0f, 100f)]
    private float pieDropChancePercent = 3f;

    public int MaxHealth => maxHealth;
    public float Damage => damage;
    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public Color HitFlashColor => hitFlashColor;
    public float HitFlashDuration => hitFlashDuration;
    public float MoveSpeed => moveSpeed;
    public MonsterChaseSettings Chase => chase;
    public MonsterContactAttackSettings ContactAttack =>
        contactAttack;
    public MonsterRangedAttackSettings RangedAttack =>
        rangedAttack;
    public GameObject ExperienceCrystalDropPrefab =>
        experienceCrystalDropPrefab;
    public GameObject RedBerryDropPrefab =>
        redBerryDropPrefab;
    public float RedBerryDropChancePercent =>
        redBerryDropChancePercent;
    public GameObject StarCandyDropPrefab =>
        starCandyDropPrefab;
    public float StarCandyDropChancePercent =>
        starCandyDropChancePercent;
    public GameObject PieDropPrefab =>
        pieDropPrefab;
    public float PieDropChancePercent =>
        pieDropChancePercent;

    private void OnEnable()
    {
        EnsureBehaviorSettings();
    }

    private void OnValidate()
    {
        EnsureBehaviorSettings();

        maxHealth = Mathf.Max(1, maxHealth);
        damage = Mathf.Max(0f, damage);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0.01f, knockbackDuration);
        hitFlashDuration = Mathf.Max(0f, hitFlashDuration);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        redBerryDropChancePercent = Mathf.Clamp(
            redBerryDropChancePercent,
            0f,
            100f
        );
        starCandyDropChancePercent = Mathf.Clamp(
            starCandyDropChancePercent,
            0f,
            100f
        );
        pieDropChancePercent = Mathf.Clamp(
            pieDropChancePercent,
            0f,
            100f
        );

        chase.Validate();
        contactAttack.Validate();
        rangedAttack.Validate();
    }

    private void EnsureBehaviorSettings()
    {
        if (chase == null)
        {
            chase = new MonsterChaseSettings();
        }

        if (contactAttack == null)
        {
            contactAttack =
                new MonsterContactAttackSettings();
        }

        if (rangedAttack == null)
        {
            rangedAttack =
                new MonsterRangedAttackSettings();
        }
    }
}
