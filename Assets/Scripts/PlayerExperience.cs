using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerExperience : MonoBehaviour
{
    [Header("Experience Growth")]
    [Tooltip("레벨 1에서 다음 레벨까지 필요한 경험치입니다.")]
    [SerializeField, Min(1)]
    private int baseRequiredExperience = 10;

    [Tooltip("레벨이 오를 때마다 증가하는 필요 경험치입니다.")]
    [SerializeField, Min(0)]
    private int requiredExperienceIncreasePerLevel = 5;

    [field: Header("Runtime Experience")]
    [field: SerializeField, Min(0)]
    public int CurrentExperience { get; private set; }

    [field: SerializeField, Min(1)]
    public int RequiredExperience { get; private set; } = 10;

    /// <summary>
    /// 현재 경험치 게이지 비율입니다.
    /// </summary>
    public float ExperienceRatio =>
        RequiredExperience > 0
            ? (float)CurrentExperience /
              RequiredExperience
            : 0f;

    /// <summary>
    /// 현재 경험치 또는 필요 경험치가 변경되었을 때 호출됩니다.
    /// </summary>
    public event Action<int, int> OnExperienceChanged;

    /// <summary>
    /// 경험치를 획득했을 때 호출됩니다.
    /// 전달값은 획득한 경험치입니다.
    /// </summary>
    public event Action<int> OnExperienceAdded;

    private GameManager gameManager;

    private bool isSubscribed;
    private bool isInitialized;

    private void Awake()
    {
        baseRequiredExperience = Mathf.Max(
            1,
            baseRequiredExperience
        );

        requiredExperienceIncreasePerLevel =
            Mathf.Max(
                0,
                requiredExperienceIncreasePerLevel
            );

        CurrentExperience = Mathf.Max(
            0,
            CurrentExperience
        );

        RequiredExperience =
            CalculateRequiredExperience(1);
    }

    private void OnEnable()
    {
        TrySubscribeGameManager();
    }

    private void Start()
    {
        TrySubscribeGameManager();
        InitializeIfNeeded();
    }

    private void OnDisable()
    {
        UnsubscribeGameManager();
    }

    /// <summary>
    /// 새 게임을 시작할 때 경험치를 초기화합니다.
    /// </summary>
    public void ResetExperience()
    {
        InitializeIfNeeded();

        CurrentExperience = 0;

        RequiredExperience =
            CalculateRequiredExperience(
                GetCurrentPlayerLevel()
            );

        NotifyExperienceChanged();
    }

    /// <summary>
    /// 플레이어에게 경험치를 추가합니다.
    /// 필요 경험치를 넘으면 자동으로 레벨이 상승합니다.
    /// </summary>
    public void AddExperience(int amount)
    {
        InitializeIfNeeded();

        if (amount <= 0)
        {
            return;
        }

        if (!GameManager.HasInstance)
        {
            Debug.LogWarning(
                $"{nameof(PlayerExperience)}: " +
                "GameManager를 찾을 수 없습니다.",
                this
            );

            return;
        }

        CurrentExperience += amount;
        OnExperienceAdded?.Invoke(amount);

        ProcessLevelUp();
        NotifyExperienceChanged();
    }

    /// <summary>
    /// 디버깅이나 특수 이벤트에서 현재 경험치를 직접 지정합니다.
    /// </summary>
    public void SetExperience(int amount)
    {
        InitializeIfNeeded();

        CurrentExperience = Mathf.Max(
            0,
            amount
        );

        ProcessLevelUp();
        NotifyExperienceChanged();
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        TrySubscribeGameManager();

        RequiredExperience =
            CalculateRequiredExperience(
                GetCurrentPlayerLevel()
            );

        CurrentExperience = Mathf.Clamp(
            CurrentExperience,
            0,
            RequiredExperience - 1
        );

        isInitialized = true;

        NotifyExperienceChanged();
    }

    /// <summary>
    /// 현재 경험치로 가능한 만큼 연속 레벨 업합니다.
    /// 남은 경험치는 다음 레벨로 이월됩니다.
    /// </summary>
    private void ProcessLevelUp()
    {
        if (!GameManager.HasInstance)
        {
            return;
        }

        while (CurrentExperience >=
               RequiredExperience)
        {
            int consumedExperience =
                RequiredExperience;

            int previousLevel =
                GameManager.Instance
                    .CurrentPlayerLevel;

            CurrentExperience -=
                consumedExperience;

            GameManager.Instance.LevelUp();

            int currentLevel =
                GameManager.Instance
                    .CurrentPlayerLevel;

            // 추후 최대 레벨이 추가되어
            // 레벨 업이 거부되는 경우를 대비합니다.
            if (currentLevel == previousLevel)
            {
                CurrentExperience +=
                    consumedExperience;

                break;
            }

            RequiredExperience =
                CalculateRequiredExperience(
                    currentLevel
                );
        }
    }

    /// <summary>
    /// 해당 레벨에서 다음 레벨까지 필요한 경험치를 계산합니다.
    /// </summary>
    private int CalculateRequiredExperience(
        int currentLevel
    )
    {
        currentLevel = Mathf.Max(
            1,
            currentLevel
        );

        int levelOffset =
            currentLevel - 1;

        return Mathf.Max(
            1,
            baseRequiredExperience +
            requiredExperienceIncreasePerLevel *
            levelOffset
        );
    }

    private int GetCurrentPlayerLevel()
    {
        if (!GameManager.HasInstance)
        {
            return 1;
        }

        return GameManager.Instance
            .CurrentPlayerLevel;
    }

    private void TrySubscribeGameManager()
    {
        if (isSubscribed ||
            !GameManager.HasInstance)
        {
            return;
        }

        gameManager =
            GameManager.Instance;

        gameManager.OnPlayerLevelChanged +=
            HandlePlayerLevelChanged;

        isSubscribed = true;
    }

    private void UnsubscribeGameManager()
    {
        if (!isSubscribed ||
            gameManager == null)
        {
            return;
        }

        gameManager.OnPlayerLevelChanged -=
            HandlePlayerLevelChanged;

        gameManager = null;
        isSubscribed = false;
    }

    /// <summary>
    /// 외부에서 플레이어 레벨이 변경된 경우
    /// 필요 경험치를 다시 계산합니다.
    /// </summary>
    private void HandlePlayerLevelChanged(
        int newLevel
    )
    {
        RequiredExperience =
            CalculateRequiredExperience(
                newLevel
            );

        NotifyExperienceChanged();
    }

    private void NotifyExperienceChanged()
    {
        OnExperienceChanged?.Invoke(
            CurrentExperience,
            RequiredExperience
        );
    }
}