using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SFXPlaybackProfile",
    menuName = "Audio/SFX Playback Profile"
)]
public sealed class SFXPlaybackProfile : ScriptableObject
{
    [Header("Voice Pool")]
    [SerializeField, Min(1)]
    [Tooltip("동시에 재생할 수 있는 SFX AudioSource 수입니다.")]
    private int voicePoolSize = 8;

    [Header("Per-SFX Settings")]
    [SerializeField]
    private SFXPlaybackSettings[] settings =
        Array.Empty<SFXPlaybackSettings>();

    public int VoicePoolSize =>
        voicePoolSize;

    public bool TryGetSettings(
        ESFXType sfxType,
        out SFXPlaybackSettings result
    )
    {
        for (int i = 0; i < settings.Length; i++)
        {
            SFXPlaybackSettings candidate = settings[i];

            if (candidate != null &&
                candidate.SfxType == sfxType)
            {
                result = candidate;
                return true;
            }
        }

        result = null;
        return false;
    }

    private void OnValidate()
    {
        voicePoolSize = Mathf.Max(1, voicePoolSize);

        for (int i = 0; i < settings.Length; i++)
        {
            settings[i]?.Validate();
        }
    }
}

[Serializable]
public sealed class SFXPlaybackSettings
{
    [SerializeField]
    private ESFXType sfxType;

    [Header("Pitch")]
    [SerializeField, Range(0.1f, 3f)]
    private float minPitch = 1f;

    [SerializeField, Range(0.1f, 3f)]
    private float maxPitch = 1f;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)]
    private float minVolumeScale = 1f;

    [SerializeField, Range(0f, 1f)]
    private float maxVolumeScale = 1f;

    [Header("Rapid Retrigger")]
    [SerializeField, Min(0f)]
    [Tooltip("같은 SFX가 다시 재생되기까지 필요한 최소 시간입니다.")]
    private float minRetriggerInterval;

    public ESFXType SfxType =>
        sfxType;

    public float MinPitch =>
        minPitch;

    public float MaxPitch =>
        maxPitch;

    public float MinVolumeScale =>
        minVolumeScale;

    public float MaxVolumeScale =>
        maxVolumeScale;

    public float MinRetriggerInterval =>
        minRetriggerInterval;

    public float PickPitch(
        float fallbackPitch,
        bool hasPreviousPitch,
        float previousPitch
    )
    {
        if (maxPitch < minPitch)
        {
            return fallbackPitch;
        }

        float firstPitch = UnityEngine.Random.Range(
            minPitch,
            maxPitch
        );

        if (!hasPreviousPitch ||
            Mathf.Approximately(minPitch, maxPitch))
        {
            return firstPitch;
        }

        float secondPitch = UnityEngine.Random.Range(
            minPitch,
            maxPitch
        );

        return Mathf.Abs(firstPitch - previousPitch) >=
            Mathf.Abs(secondPitch - previousPitch)
                ? firstPitch
                : secondPitch;
    }

    public float PickVolumeScale()
    {
        return UnityEngine.Random.Range(
            minVolumeScale,
            maxVolumeScale
        );
    }

    internal void Validate()
    {
        minPitch = Mathf.Clamp(minPitch, 0.1f, 3f);
        maxPitch = Mathf.Clamp(maxPitch, minPitch, 3f);
        minVolumeScale = Mathf.Clamp01(minVolumeScale);
        maxVolumeScale = Mathf.Clamp(
            maxVolumeScale,
            minVolumeScale,
            1f
        );
        minRetriggerInterval = Mathf.Max(
            0f,
            minRetriggerInterval
        );
    }
}
