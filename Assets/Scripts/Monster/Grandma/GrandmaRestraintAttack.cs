using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
public sealed class GrandmaRestraintAttack : MonoBehaviour
{
    [SerializeField]
    private GrandmaBossProfile profile;

    private MonsterHealth monsterHealth;
    private MonsterSanityAppearance appearance;
    private PlayerMental playerMental;

    private float nextAttackTime;
    private float attackAnimationEndTime = -1f;

    public GrandmaBossProfile Profile => profile;

    public float AttackCooldown
    {
        get
        {
            MonsterContactAttackSettings settings =
                GetSettings();

            return settings != null
                ? settings.AttackCooldown
                : 0.75f;
        }
    }

    public float RestraintDuration =>
        profile != null
            ? profile.RestraintDuration
            : 1.5f;

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        appearance = GetComponent<MonsterSanityAppearance>();
        ResolvePlayerMental();
    }

    private void OnEnable()
    {
        nextAttackTime = Time.time;
    }

    private void OnDisable()
    {
        RestoreMovementAnimation();
    }

    private void Update()
    {
        if (attackAnimationEndTime < 0f ||
            Time.time < attackAnimationEndTime)
        {
            return;
        }

        RestoreMovementAnimation();
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        TryAttack(collision.collider);
    }

    private void OnCollisionStay2D(
        Collision2D collision
    )
    {
        TryAttack(collision.collider);
    }

    private void TryAttack(Collider2D other)
    {
        if (other == null ||
            monsterHealth == null ||
            monsterHealth.IsDead ||
            Time.time < nextAttackTime)
        {
            return;
        }

        if (GameManager.HasInstance &&
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        PlayerMovement playerMovement =
            other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null ||
            !playerMovement.CompareTag("Player"))
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
        ApplyRestraint(playerMovement);

        nextAttackTime =
            Time.time + AttackCooldown;
        PlayAttackAnimation();
    }

    private void ApplyRestraint(
        PlayerMovement playerMovement
    )
    {
        BlanketRestraintEffect restraint =
            playerMovement.GetComponent
                <BlanketRestraintEffect>();

        if (restraint == null)
        {
            restraint = playerMovement.gameObject
                .AddComponent<BlanketRestraintEffect>();
        }

        restraint.Apply(RestraintDuration);
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

    private void PlayAttackAnimation()
    {
        if (appearance == null)
        {
            return;
        }

        appearance.SetMotionState(
            MonsterSanityAppearance.MonsterMotionState.Attack
        );

        MonsterContactAttackSettings settings =
            GetSettings();
        float duration = settings != null
            ? settings.AttackAnimationDuration
            : 0.25f;

        attackAnimationEndTime =
            Time.time + duration;
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

    private MonsterContactAttackSettings GetSettings()
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
}
