using UnityEngine;

public sealed class MonsterContactAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField, Min(0.05f)]
    private float attackCooldown = 0.75f;

    [SerializeField, Min(0f)]
    private float attackAnimationDuration = 0.25f;

    private MonsterHealth monsterHealth;
    private MonsterSanityAppearance appearance;

    private float nextAttackTime;
    private float attackAnimationEndTime = -1f;

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        appearance = GetComponent<MonsterSanityAppearance>();
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
        if (GameManager.HasInstance && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        if (Time.time < nextAttackTime || monsterHealth.IsDead)
        {
            return;
        }

        PlayerMental player = collision.gameObject.GetComponentInParent<PlayerMental>();

        if (player == null || player.IsDepleted)
        {
            return;
        }

        player.TakeMentalDamage(monsterHealth.Damage);

        nextAttackTime = Time.time + attackCooldown;

        PlayAttackAnimation();
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