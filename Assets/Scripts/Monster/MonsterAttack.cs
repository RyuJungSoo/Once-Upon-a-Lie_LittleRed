using UnityEngine;

public sealed class MonsterAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField, Min(0.05f)]
    private float attackCooldown = 0.75f;

    [SerializeField, Min(0f)]
    private float attackAnimationDuration = 0.25f;

    private MonsterHealth monsterHealth;
    private MonsterSanityAppearance appearance;
    private PlayerMental playerMental;

    private float nextAttackTime;
    private float attackAnimationEndTime = -1f;

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

        appearance.SetMotionState(MonsterSanityAppearance.MonsterMotionState.Run);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 실제 Player 콜라이더와 충돌했는지 먼저 확인
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (GameManager.HasInstance && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        if (Time.time < nextAttackTime || monsterHealth == null || monsterHealth.IsDead)
        {
            return;
        }

        ResolvePlayerMental();

        if (playerMental == null || playerMental.IsDepleted)
        {
            return;
        }

        playerMental.TakeMentalDamage(monsterHealth.Damage);

        nextAttackTime = Time.time + attackCooldown;

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

        attackAnimationEndTime = Time.time + attackAnimationDuration;
    }
}