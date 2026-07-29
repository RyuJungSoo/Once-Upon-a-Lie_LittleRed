using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
[RequireComponent(typeof(MonsterAttack))]
[RequireComponent(typeof(MothFanAttack))]
[RequireComponent(typeof(SignZigzagAttack))]
public sealed partial class HunterDirector : MonoBehaviour
{
    public enum AttackPattern
    {
        Bird,
        Moth,
        Sign,
        Recovery
    }

    [SerializeField]
    private HunterBossProfile profile;

    private Rigidbody2D body;
    private MonsterHealth monsterHealth;
    private MonsterChase chase;
    private MonsterAttack birdAttack;
    private MothFanAttack mothAttack;
    private SignZigzagAttack signAttack;

    private AttackPattern currentPattern;
    private float patternEndTime;
    private int nextPatternIndex = -1;

    public HunterBossProfile Profile => profile;
    public AttackPattern CurrentPattern =>
        currentPattern;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        chase = GetComponent<MonsterChase>();
        birdAttack = GetComponent<MonsterAttack>();
        mothAttack = GetComponent<MothFanAttack>();
        signAttack = GetComponent<SignZigzagAttack>();

        mothAttack.enabled = false;
        signAttack.enabled = false;
    }

    private void OnEnable()
    {
        nextPatternIndex = -1;
        EnterNextPattern();
    }

    private void OnDisable()
    {
        if (mothAttack != null)
        {
            mothAttack.enabled = false;
        }

        if (signAttack != null)
        {
            signAttack.enabled = false;
        }

        if (birdAttack != null)
        {
            birdAttack.enabled = true;
        }

        if (chase != null)
        {
            chase.enabled = true;
        }
    }
}
