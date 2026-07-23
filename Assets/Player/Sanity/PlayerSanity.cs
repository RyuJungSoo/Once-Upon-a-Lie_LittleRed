using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSanity : MonoBehaviour
{
    public const float MaxSanity = 100f;

    [Header("Sanity")]
    [SerializeField, Range(0f, MaxSanity)]
    [Tooltip("플레이어의 현재 정신력이야.")]
    private float currentSanity = MaxSanity;

    [Header("Stage Thresholds")]
    [SerializeField, Range(0f, MaxSanity)]
    [Tooltip("이 값 이하면 Low 단계야.")]
    private float lowStageThreshold = 33f;

    [SerializeField, Range(0f, MaxSanity)]
    [Tooltip("이 값 미만이면 Middle, 이상이면 High 단계야.")]
    private float highStageThreshold = 67f;

    private SanityStage currentStage;
    private float lastPublishedSanity;
    private SanityStage lastPublishedStage;

    public enum SanityStage
    {
        Low,
        Middle,
        High
    }

    public float CurrentSanity => currentSanity;
    public float NormalizedSanity => currentSanity / MaxSanity;
    public SanityStage CurrentStage => currentStage;
    public bool IsDepleted => currentSanity <= 0f;

    public event Action<float, float> SanityChanged;
    public event Action<SanityStage, SanityStage> SanityStageChanged;
    public event Action SanityDepleted;

    private void Awake()
    {
        ClampInspectorValues();
        currentStage = EvaluateStage(currentSanity);
        lastPublishedSanity = currentSanity;
        lastPublishedStage = currentStage;
    }

    private void Update()
    {
        ClampInspectorValues();
        currentStage = EvaluateStage(currentSanity);

        if (!Mathf.Approximately(lastPublishedSanity, currentSanity)
            || lastPublishedStage != currentStage)
        {
            PublishSanityChanges();
        }
    }

    private void OnValidate()
    {
        ClampInspectorValues();
        currentStage = EvaluateStage(currentSanity);
    }

    public void SetSanity(float value)
    {
        currentSanity = Mathf.Clamp(value, 0f, MaxSanity);
        currentStage = EvaluateStage(currentSanity);

        PublishSanityChanges();
    }

    private void PublishSanityChanges()
    {
        float previousSanity = lastPublishedSanity;
        SanityStage previousStage = lastPublishedStage;

        lastPublishedSanity = currentSanity;
        lastPublishedStage = currentStage;

        if (!Mathf.Approximately(previousSanity, currentSanity))
        {
            SanityChanged?.Invoke(previousSanity, currentSanity);
        }

        if (previousStage != currentStage)
        {
            SanityStageChanged?.Invoke(previousStage, currentStage);
        }

        if (previousSanity > 0f && IsDepleted)
        {
            SanityDepleted?.Invoke();
        }
    }

    public void ReduceSanity(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetSanity(currentSanity - amount);
    }

    public void RestoreSanity(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetSanity(currentSanity + amount);
    }

    public bool TrySpendSanity(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentSanity < amount)
        {
            return false;
        }

        SetSanity(currentSanity - amount);
        return true;
    }

    public void RestoreToMaximum()
    {
        SetSanity(MaxSanity);
    }

    private SanityStage EvaluateStage(float sanity)
    {
        if (sanity <= lowStageThreshold)
        {
            return SanityStage.Low;
        }

        if (sanity < highStageThreshold)
        {
            return SanityStage.Middle;
        }

        return SanityStage.High;
    }

    private void ClampInspectorValues()
    {
        currentSanity = Mathf.Clamp(currentSanity, 0f, MaxSanity);
        lowStageThreshold = Mathf.Clamp(lowStageThreshold, 0f, MaxSanity);
        highStageThreshold = Mathf.Clamp(highStageThreshold, lowStageThreshold, MaxSanity);
    }
}
