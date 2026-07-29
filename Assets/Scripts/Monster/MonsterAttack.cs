using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
public sealed class MonsterAttack : MonoBehaviour
{
    private MonsterHealth monsterHealth;
    private MonsterSanityAppearance appearance;
    private PlayerMental playerMental;

    private float nextAttackTime;
    private float attackAnimationEndTime = -1f;

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

    public float AttackAnimationDuration
    {
        get
        {
            MonsterContactAttackSettings settings =
                GetSettings();

            return settings != null
                ? settings.AttackAnimationDuration
                : 0.25f;
        }
    }

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        appearance = GetComponent<MonsterSanityAppearance>();

        ResolvePlayerMental();
    }

    private void Update()
    {
        if (appearance == null || attackAnimationEndTime < 0f || Time.time < attackAnimationEndTime)
        {
            return;
        }

        attackAnimationEndTime = -1f;

        appearance.RestoreMovementMotionState();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 실제 Player 콜라이더와 충돌했는지 먼저 확인
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (Time.time < nextAttackTime || monsterHealth == null || monsterHealth.IsDead)
        {
            return;
        }

        if (GameManager.HasInstance && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        ResolvePlayerMental();

        if (playerMental == null || playerMental.IsDepleted)
        {
            return;
        }

        playerMental.TakeMentalDamage(monsterHealth.Damage);

        PlayerMovement playerMovement = collision.gameObject.GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ApplyKnockback(transform.position);
        }

        nextAttackTime = Time.time + AttackCooldown;

        PlayAttackAnimation();
    }

    private void ResolvePlayerMental()
    {
        if (playerMental != null)
        {
            return;
        }

        // 현재 프로젝트에서는 PlayerMental이 GameManager에 붙어 있음
        if (GameManager.HasInstance)
        {
            playerMental = GameManager.Instance.GetComponent<PlayerMental>();
        }

        // Awake 실행 순서 때문에 GameManager가 아직 준비되지 않은 경우
        if (playerMental == null)
        {
            playerMental = FindFirstObjectByType<PlayerMental>();
        }
    }

    private void PlayAttackAnimation()
    {
        if (appearance == null)
        {
            return;
        }

        appearance.SetMotionState(MonsterSanityAppearance.MonsterMotionState.Attack);

        attackAnimationEndTime =
            Time.time + AttackAnimationDuration;
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
