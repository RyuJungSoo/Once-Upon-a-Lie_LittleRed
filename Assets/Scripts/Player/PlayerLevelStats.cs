using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerLevelStats : MonoBehaviour
{
    [Header("Attack Growth")]
    [SerializeField, Min(0f)]
    private float baseAttackPower = 10f;

    [SerializeField, Min(0f)]
    private float attackPowerPerLevel = 1f;

    [SerializeField, Min(0f)]
    private float baseBulletSpeed = 10f;

    [SerializeField, Min(0f)]
    private float bulletSpeedPerLevel = 0.25f;

    [SerializeField, Min(0)]
    private int basePenetration;

    [SerializeField, Min(1)]
    private int penetrationLevelInterval = 5;

    [SerializeField, Min(0)]
    private int penetrationIncrease = 1;

    [SerializeField, Min(1)]
    private int baseMagazineCapacity = 10;

    [SerializeField, Min(1)]
    private int magazineLevelInterval = 5;

    [SerializeField, Min(0)]
    private int magazineIncrease = 2;

    [Header("Survival Growth")]
    [SerializeField, Min(1)]
    private int baseMaxMental = 100;

    [SerializeField, Min(0)]
    private int maxMentalPerLevel = 5;

    [SerializeField, Min(0f)]
    private float baseMoveSpeed = 5f;

    [SerializeField, Min(0f)]
    private float moveSpeedPerLevel = 0.1f;

    [Header("Mental Growth")]
    [Tooltip("레벨 1에서 초당 자연 감소하는 Mental입니다.")]
    [SerializeField, Min(0f)]
    private float basePassiveMentalDrain = 0.5f;

    [Tooltip("레벨이 오를 때마다 자연 감소량이 줄어드는 수치입니다.")]
    [SerializeField, Min(0f)]
    private float passiveDrainReductionPerLevel = 0.015f;

    [SerializeField, Min(0f)]
    private float minimumPassiveMentalDrain = 0.1f;

    [Tooltip("레벨이 오를 때마다 피격 Mental 피해 배율이 감소합니다.")]
    [SerializeField, Range(0f, 1f)]
    private float hitMentalDamageReductionPerLevel = 0.02f;

    [Tooltip("피격 Mental 피해 배율의 최솟값입니다.")]
    [SerializeField, Range(0f, 1f)]
    private float minimumHitMentalDamageMultiplier = 0.5f;

    [Tooltip("Low Mental 상태에서 레벨당 증가하는 공격력 배율입니다.")]
    [SerializeField, Min(0f)]
    private float lowMentalAttackBonusPerLevel = 0.03f;

    public int CurrentLevel { get; private set; } = 1;

    public float AttackPower { get; private set; }
    public float BulletSpeed { get; private set; }
    public int Penetration { get; private set; }
    public int MagazineCapacity { get; private set; }

    public int MaxMental { get; private set; }
    public float MoveSpeed { get; private set; }

    public float PassiveMentalDrainPerSecond { get; private set; }
    public float HitMentalDamageMultiplier { get; private set; }
    public float LowMentalAttackMultiplier { get; private set; }

    public event Action OnStatsChanged;

    private GameManager gameManager;
    private bool isSubscribed;

    private void Awake()
    {
        RecalculateStats(1);
    }

    private void OnEnable()
    {
        TrySubscribeGameManager();
    }

    private void Start()
    {
        TrySubscribeGameManager();

        if (GameManager.HasInstance)
        {
            RecalculateStats(
                GameManager.Instance.CurrentPlayerLevel
            );
        }
    }

    private void OnDisable()
    {
        UnsubscribeGameManager();
    }

    private void TrySubscribeGameManager()
    {
        if (isSubscribed || !GameManager.HasInstance)
        {
            return;
        }

        gameManager = GameManager.Instance;
        gameManager.OnPlayerLevelChanged += HandlePlayerLevelChanged;

        isSubscribed = true;
    }

    private void UnsubscribeGameManager()
    {
        if (!isSubscribed || gameManager == null)
        {
            return;
        }

        gameManager.OnPlayerLevelChanged -= HandlePlayerLevelChanged;

        gameManager = null;
        isSubscribed = false;
    }

    private void HandlePlayerLevelChanged(int newLevel)
    {
        RecalculateStats(newLevel);
    }

    public void RecalculateStats(int level)
    {
        CurrentLevel = Mathf.Max(1, level);

        int levelOffset = CurrentLevel - 1;

        int penetrationUpgradeCount =
            CurrentLevel / Mathf.Max(1, penetrationLevelInterval);

        int magazineUpgradeCount =
            CurrentLevel / Mathf.Max(1, magazineLevelInterval);

        AttackPower =
            baseAttackPower +
            attackPowerPerLevel * levelOffset;

        BulletSpeed =
            baseBulletSpeed +
            bulletSpeedPerLevel * levelOffset;

        Penetration =
            basePenetration +
            penetrationUpgradeCount * penetrationIncrease;

        MagazineCapacity =
            baseMagazineCapacity +
            magazineUpgradeCount * magazineIncrease;

        MaxMental =
            baseMaxMental +
            maxMentalPerLevel * levelOffset;

        MoveSpeed =
            baseMoveSpeed +
            moveSpeedPerLevel * levelOffset;

        PassiveMentalDrainPerSecond = Mathf.Max(
            minimumPassiveMentalDrain,
            basePassiveMentalDrain -
            passiveDrainReductionPerLevel * levelOffset
        );

        HitMentalDamageMultiplier = Mathf.Max(
            minimumHitMentalDamageMultiplier,
            1f -
            hitMentalDamageReductionPerLevel * levelOffset
        );

        LowMentalAttackMultiplier =
            1f +
            lowMentalAttackBonusPerLevel * levelOffset;

        OnStatsChanged?.Invoke();
    }
}
