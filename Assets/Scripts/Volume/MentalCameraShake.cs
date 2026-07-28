using Unity.Cinemachine;
using UnityEngine;

public sealed class MentalCameraShake : MonoBehaviour
{
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

    private CinemachineBasicMultiChannelPerlin noise;
    private float targetAmplitude;
    private float targetFrequency;

    private void Awake()
    {
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
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
            enabled = false;
            return;
        }

        HandleMentalStateChanged(playerMental.CurrentMentalState);

        noise.AmplitudeGain = targetAmplitude;
        noise.FrequencyGain = targetFrequency;
    }

    private void Update()
    {
        noise.AmplitudeGain = Mathf.MoveTowards(noise.AmplitudeGain, targetAmplitude, transitionSpeed * Time.deltaTime);

        noise.FrequencyGain = Mathf.MoveTowards(noise.FrequencyGain,targetFrequency,transitionSpeed * Time.deltaTime);
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
}