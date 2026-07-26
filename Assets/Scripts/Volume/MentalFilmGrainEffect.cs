using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public sealed class MentalFilmGrainEffect : MentalVolumeEffect
{
    [Tooltip("정신력이 최대일 때 필름 그레인 강도")]
    [SerializeField, Range(0f, 1f)]
    private float minIntensity;

    [Tooltip("정신력이 0일 때 필름 그레인 강도")]
    [SerializeField, Range(0f, 1f)]
    private float maxIntensity = 0.5f;

    private FilmGrain filmGrain;

    private void OnValidate()
    {
        minIntensity = Mathf.Clamp01(minIntensity);
        maxIntensity = Mathf.Clamp(
            maxIntensity,
            minIntensity,
            1f
        );
    }

    public override void Initialize(
        VolumeProfile runtimeProfile
    )
    {
        if (runtimeProfile == null)
        {
            filmGrain = null;
            return;
        }

        if (!runtimeProfile.TryGet(out filmGrain))
        {
            Debug.LogWarning(
                "Global Volume Profile에 Film Grain이 없어.",
                this
            );
            return;
        }

        filmGrain.intensity.Override(minIntensity);
    }

    public override void Apply(float dangerRatio)
    {
        if (filmGrain == null)
        {
            return;
        }

        float intensity = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            Mathf.Clamp01(dangerRatio)
        );

        filmGrain.intensity.Override(intensity);
    }
}
