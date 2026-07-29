using System.Reflection;
using UnityEngine;

public sealed class MentalCameraShake : MonoBehaviour
{
    private const string CinemachineNoiseTypeName =
        "Unity.Cinemachine.CinemachineBasicMultiChannelPerlin";

    [Header("Mental Source")]
    [SerializeField] private PlayerMental playerMental;

    [Header("High Mental")]
    [SerializeField, Min(0f)] private float highAmplitude = 0f;
    [SerializeField, Min(0f)] private float highFrequency = 1f;

    [Header("Medium Mental")]
    [SerializeField, Min(0f)] private float mediumAmplitude = 0.1f;
    [SerializeField, Min(0f)] private float mediumFrequency = 0.8f;

    [Header("Low Mental")]
    [SerializeField, Min(0f)] private float lowAmplitude = 0.3f;
    [SerializeField, Min(0f)] private float lowFrequency = 1.2f;

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float transitionSpeed = 1.5f;

    private Component noise;
    private FieldInfo amplitudeGainField;
    private FieldInfo frequencyGainField;
    private float targetAmplitude;
    private float targetFrequency;

    private void Awake()
    {
        if (TryCacheNoiseComponent())
        {
            return;
        }

        Debug.LogWarning(
            $"[{nameof(MentalCameraShake)}] Cinemachine 노이즈 컴포넌트를 찾을 수 없어 " +
            "카메라 흔들림 기능만 비활성화합니다. 프로젝트는 Cinemachine 없이도 실행할 수 있습니다.",
            this);
        enabled = false;
    }

    private void OnEnable()
    {
        if (playerMental != null)
        {
            playerMental.OnMentalStateChanged += HandleMentalStateChanged;
        }
    }

    private void Start()
    {
        if (playerMental == null)
        {
            Debug.LogWarning(
                $"[{nameof(MentalCameraShake)}] PlayerMental이 연결되지 않아 카메라 흔들림 기능을 비활성화합니다.",
                this);
            enabled = false;
            return;
        }

        HandleMentalStateChanged(playerMental.CurrentMentalState);

        SetNoise(targetAmplitude, targetFrequency);
    }

    private void Update()
    {
        float amplitude = (float)amplitudeGainField.GetValue(noise);
        float frequency = (float)frequencyGainField.GetValue(noise);
        float maxDelta = transitionSpeed * Time.deltaTime;

        SetNoise(
            Mathf.MoveTowards(amplitude, targetAmplitude, maxDelta),
            Mathf.MoveTowards(frequency, targetFrequency, maxDelta));
    }

    private void OnDisable()
    {
        if (playerMental != null)
        {
            playerMental.OnMentalStateChanged -= HandleMentalStateChanged;
        }
    }

    private void HandleMentalStateChanged(EMentalState mentalState)
    {
        switch (mentalState)
        {
            case EMentalState.High:
                SetTargetNoise(highAmplitude,highFrequency);
                break;

            case EMentalState.Medium:
                SetTargetNoise(mediumAmplitude,mediumFrequency);
                break;

            case EMentalState.Low:
                SetTargetNoise(lowAmplitude, lowFrequency);
                break;

            default:
                SetTargetNoise(0f, 1f);
                break;
        }
    }

    private void SetTargetNoise(float amplitude,float frequency)
    {
        targetAmplitude = amplitude;
        targetFrequency = frequency;
    }

    private bool TryCacheNoiseComponent()
    {
        Component[] components = GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null || component.GetType().FullName != CinemachineNoiseTypeName)
            {
                continue;
            }

            noise = component;
            amplitudeGainField = component.GetType().GetField(
                "AmplitudeGain",
                BindingFlags.Instance | BindingFlags.Public);
            frequencyGainField = component.GetType().GetField(
                "FrequencyGain",
                BindingFlags.Instance | BindingFlags.Public);

            if (amplitudeGainField != null && frequencyGainField != null)
            {
                return true;
            }

            Debug.LogWarning(
                $"[{nameof(MentalCameraShake)}] 현재 Cinemachine 버전에서 필요한 노이즈 값을 찾을 수 없습니다.",
                this);
            return false;
        }

        return false;
    }

    private void SetNoise(float amplitude, float frequency)
    {
        amplitudeGainField.SetValue(noise, amplitude);
        frequencyGainField.SetValue(noise, frequency);
    }
}
