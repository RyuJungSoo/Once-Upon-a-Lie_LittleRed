using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public sealed class MentalVignetteEffect : MentalVolumeEffect
{
    [Tooltip("정신력이 최대일 때 비네트 강도")]
    [SerializeField, Range(0f, 1f)]
    private float minIntensity;

    [Tooltip("정신력이 0일 때 비네트 강도")]
    [SerializeField, Range(0f, 1f)]
    private float maxIntensity = 0.55f;

    private Vignette vignette;

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
            vignette = null;
            return;
        }

        if (!runtimeProfile.TryGet(out vignette))
        {
            Debug.LogWarning(
                "Global Volume Profile에 Vignette가 없어.",
                this
            );
            return;
        }

        vignette.intensity.Override(minIntensity);
    }

    public override void Apply(float dangerRatio)
    {
        if (vignette == null)
        {
            return;
        }

        float intensity = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            Mathf.Clamp01(dangerRatio)
        );

        vignette.intensity.Override(intensity);
    }
}
