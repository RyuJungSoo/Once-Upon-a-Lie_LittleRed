using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
[RequireComponent(typeof(MonsterAttack))]
[RequireComponent(typeof(TeaCupBarrageAttack))]
[RequireComponent(typeof(GrandmaRestraintAttack))]
public sealed partial class GrandmaDirector : MonoBehaviour
{
    public enum AttackPattern
    {
        TeaCup,
        Blanket,
        RedString,
        Recovery
    }

    [SerializeField]
    private GrandmaBossProfile profile;

    private Rigidbody2D body;
    private MonsterHealth monsterHealth;
    private MonsterChase chase;
    private MonsterAttack redStringAttack;
    private TeaCupBarrageAttack teaCupAttack;
    private GrandmaRestraintAttack restraintAttack;

    private AttackPattern currentPattern;
    private float patternEndTime;
    private int nextPatternIndex = -1;

    public GrandmaBossProfile Profile => profile;
    public AttackPattern CurrentPattern =>
        currentPattern;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        chase = GetComponent<MonsterChase>();
        redStringAttack = GetComponent<MonsterAttack>();
        teaCupAttack =
            GetComponent<TeaCupBarrageAttack>();
        restraintAttack =
            GetComponent<GrandmaRestraintAttack>();

        teaCupAttack.enabled = false;
        restraintAttack.enabled = false;
    }

    private void OnEnable()
    {
        nextPatternIndex = -1;
        EnterNextPattern();
    }

    private void OnDisable()
    {
        if (teaCupAttack != null)
        {
            teaCupAttack.enabled = false;
        }

        if (restraintAttack != null)
        {
            restraintAttack.enabled = false;
        }

        if (redStringAttack != null)
        {
            redStringAttack.enabled = true;
        }

        if (chase != null)
        {
            chase.enabled = true;
        }
    }
}
