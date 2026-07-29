using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
[RequireComponent(typeof(MonsterAttack))]
[RequireComponent(typeof(MonsterRangedAttack))]
[RequireComponent(typeof(MonsterAimedChargeAttack))]
public sealed partial class DeerKingDirector : MonoBehaviour
{
    public enum AttackPattern
    {
        Ram,
        Ranged,
        AimedCharge,
        Recovery
    }

    [SerializeField]
    private DeerKingBossProfile profile;

    private Rigidbody2D body;
    private MonsterHealth monsterHealth;
    private MonsterChase chase;
    private MonsterAttack contactAttack;
    private MonsterRangedAttack rangedAttack;
    private MonsterAimedChargeAttack aimedCharge;

    private AttackPattern currentPattern;
    private float patternEndTime;
    private int nextPatternIndex = -1;
    private bool chargeStarted;

    public DeerKingBossProfile Profile => profile;
    public AttackPattern CurrentPattern =>
        currentPattern;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        chase = GetComponent<MonsterChase>();
        contactAttack = GetComponent<MonsterAttack>();
        rangedAttack =
            GetComponent<MonsterRangedAttack>();
        aimedCharge =
            GetComponent<MonsterAimedChargeAttack>();

        aimedCharge.SetAutomaticActivation(false);
        rangedAttack.enabled = false;
    }

    private void OnEnable()
    {
        nextPatternIndex = -1;
        EnterNextPattern();
    }

    private void OnDisable()
    {
        if (rangedAttack != null)
        {
            rangedAttack.enabled = false;
        }

        aimedCharge?.CancelAttack();

        if (contactAttack != null)
        {
            contactAttack.enabled = true;
        }

        if (chase != null)
        {
            chase.enabled = true;
        }
    }
}
