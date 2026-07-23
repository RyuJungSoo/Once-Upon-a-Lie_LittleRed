using System;
using UnityEngine;

public enum EMentalState
{
    High,
    Medium,
    Low
}

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerLevelStats))]
public class PlayerMental : MonoBehaviour
{
    public float CurrentMental { get; private set; }
    public float MaxMental { get; private set; }

    /// 현재 정신력 비율입니다. 0~1 범위입니다.
    public float MentalRatio =>
        MaxMental > 0f
            ? CurrentMental / MaxMental
            : 0f;

    /// 현재 정신력 상태입니다.
    public EMentalState CurrentMentalState =>
        GetMentalState(MentalRatio);

    /// 정신력이 모두 소진되었는지 여부입니다.
    public bool IsDepleted =>
        CurrentMental <= 0f;

    /// 현재 정신력 상태에 따른 공격력 배율입니다.
    /// Low 상태일 때 정신력 강화 효과가 적용됩니다.
    public float CurrentAttackPowerMultiplier =>
        CurrentMentalState == EMentalState.Low
            ? levelStats.LowMentalAttackMultiplier
            : 1f;

    /// 정신력 수치가 변경되었을 때 호출됩니다.
    public event Action<float, float> OnMentalChanged;

    /// 정신력 상태가 변경되었을 때 호출됩니다.
    public event Action<EMentalState> OnMentalStateChanged;

    /// 정신력이 0이 되었을 때 호출됩니다.
    public event Action OnMentalDepleted;

    private PlayerLevelStats levelStats;

    private EMentalState previousMentalState;
    private bool isInitialized;
    private bool hasDepleted;

    private void Awake()
    {
        levelStats = GetComponent<PlayerLevelStats>();
    }

    private void OnEnable()
    {
        if (levelStats == null)
        {
            levelStats = GetComponent<PlayerLevelStats>();
        }

        if (levelStats != null)
        {
            levelStats.OnStatsChanged += HandleStatsChanged;
        }
    }

    private void Start()
    {
        InitializeIfNeeded();
        NotifyMentalChanged(true);
    }

    private void OnDisable()
    {
        if (levelStats != null)
        {
            levelStats.OnStatsChanged -= HandleStatsChanged;
        }
    }

    private void Update()
    {
        UpdatePassiveDrain();
    }

    /// <summary>
    /// 새 게임을 시작할 때 정신력을 최대치로 초기화합니다.
    /// </summary>
    public void ResetMental()
    {
        InitializeIfNeeded();

        hasDepleted = false;
        CurrentMental = MaxMental;

        NotifyMentalChanged(true);
    }

    /// <summary>
    /// 적 공격이나 접촉으로 정신력 피해를 받습니다.
    /// 정신력 강화에 따른 피격 피해 완화가 적용됩니다.
    /// </summary>
    public void TakeMentalDamage(float baseDamage)
    {
        InitializeIfNeeded();

        if (baseDamage <= 0f || IsDepleted)
        {
            return;
        }

        float damageMultiplier =
            levelStats.HitMentalDamageMultiplier;

        float finalDamage = Mathf.Max(
            0f,
            baseDamage * damageMultiplier
        );

        SetMental(CurrentMental - finalDamage);
    }

    /// <summary>
    /// 정신력 강화 효과를 무시하고 정신력을 직접 감소시킵니다.
    /// 강제 감소나 특수 이벤트 등에 사용합니다.
    /// </summary>
    public void DecreaseMentalRaw(float amount)
    {
        InitializeIfNeeded();

        if (amount <= 0f || IsDepleted)
        {
            return;
        }

        SetMental(CurrentMental - amount);
    }

    /// <summary>
    /// 정신력을 회복합니다.
    /// </summary>
    public void RestoreMental(float amount)
    {
        InitializeIfNeeded();

        if (amount <= 0f)
        {
            return;
        }

        SetMental(CurrentMental + amount);
    }

    /// <summary>
    /// 현재 정신력을 지정한 값으로 변경합니다.
    /// </summary>
    public void SetMental(float value)
    {
        InitializeIfNeeded();

        float newMental = Mathf.Clamp(
            value,
            0f,
            MaxMental
        );

        if (Mathf.Approximately(newMental, CurrentMental))
        {
            return;
        }

        CurrentMental = newMental;

        if (CurrentMental > 0f)
        {
            hasDepleted = false;
        }

        NotifyMentalChanged(false);
    }

    /// <summary>
    /// PlayerLevelStats의 현재 능력치를 기준으로 초기화합니다.
    /// </summary>
    private void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        if (levelStats == null)
        {
            levelStats = GetComponent<PlayerLevelStats>();
        }

        if (levelStats == null)
        {
            Debug.LogError(
                $"{nameof(PlayerMental)}에 {nameof(PlayerLevelStats)}가 없습니다.",
                this
            );
            return;
        }

        MaxMental = Mathf.Max(
            1f,
            levelStats.MaxMental
        );

        CurrentMental = MaxMental;
        previousMentalState = CurrentMentalState;

        hasDepleted = false;
        isInitialized = true;
    }

    /// <summary>
    /// 레벨 상승으로 능력치가 변경되었을 때 실행됩니다.
    /// </summary>
    private void HandleStatsChanged()
    {
        if (!isInitialized)
        {
            InitializeIfNeeded();

            if (isInitialized)
            {
                NotifyMentalChanged(true);
            }

            return;
        }

        float previousMaxMental = MaxMental;

        MaxMental = Mathf.Max(
            1f,
            levelStats.MaxMental
        );

        float maxMentalDifference =
            MaxMental - previousMaxMental;

        if (maxMentalDifference > 0f)
        {
            // 최대 정신력이 증가한 만큼 현재 정신력도 함께 증가시킵니다.
            CurrentMental = Mathf.Min(
                MaxMental,
                CurrentMental + maxMentalDifference
            );
        }
        else
        {
            CurrentMental = Mathf.Clamp(
                CurrentMental,
                0f,
                MaxMental
            );
        }

        NotifyMentalChanged(true);
    }

    /// <summary>
    /// 게임 진행 중 정신력을 실시간으로 지속 감소시킵니다.
    /// 정신력 강화에 따라 감소 속도가 완화됩니다.
    /// </summary>
    private void UpdatePassiveDrain()
    {
        if (!isInitialized)
        {
            return;
        }

        if (CurrentMental <= 1f)
        {
            return;
        }

        if (!GameManager.HasInstance ||
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        float drainPerSecond =
            levelStats.PassiveMentalDrainPerSecond;

        if (drainPerSecond <= 0f)
        {
            return;
        }

        // 자연 감소는 1에서 멈추도록 유지
        float newMental = Mathf.Max(
            1f,
            CurrentMental - drainPerSecond * Time.deltaTime
        );

        SetMental(newMental);
    }

    /// <summary>
    /// 정신력 변경 이벤트와 UI 갱신을 처리합니다.
    /// </summary>
    private void NotifyMentalChanged(bool forceStateNotification)
    {
        EMentalState newMentalState =
            CurrentMentalState;

        OnMentalChanged?.Invoke(
            CurrentMental,
            MaxMental
        );

        if (UIManager.HasInstance)
        {
            UIManager.Instance.UpdateMental(
                CurrentMental,
                MaxMental,
                newMentalState
            );
        }

        if (forceStateNotification ||
            newMentalState != previousMentalState)
        {
            previousMentalState = newMentalState;

            OnMentalStateChanged?.Invoke(
                newMentalState
            );
        }

        if (CurrentMental <= 0f &&
            !hasDepleted)
        {
            hasDepleted = true;

            OnMentalDepleted?.Invoke();

            if (GameManager.HasInstance)
            {
                GameManager.Instance.GameOver();
            }
        }
    }

    /// <summary>
    /// 현재 정신력 비율에 따라 상태를 반환합니다.
    /// </summary>
    private EMentalState GetMentalState(float mentalRatio)
    {
        float mentalPercent =
            mentalRatio * 100f;

        if (mentalPercent >= 67f)
        {
            return EMentalState.High;
        }

        if (mentalPercent >= 34f)
        {
            return EMentalState.Medium;
        }

        return EMentalState.Low;
    }
}