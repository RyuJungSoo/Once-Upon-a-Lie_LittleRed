using UnityEngine;
using UnityEngine.Rendering;

public abstract class MentalVolumeEffect : MonoBehaviour
{
    public abstract void Initialize(VolumeProfile runtimeProfile);

    public abstract void Apply(float dangerRatio);
}
