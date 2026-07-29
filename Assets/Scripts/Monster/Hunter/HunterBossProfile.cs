using UnityEngine;

[CreateAssetMenu(
    fileName = "HunterBossProfile",
    menuName = "Once Upon a Lie/Boss/Hunter Profile"
)]
public sealed class HunterBossProfile : ScriptableObject
{
    [Header("Pattern Timing")]
    [SerializeField, Min(0.1f)]
    private float birdDuration = 2.5f;

    [SerializeField, Min(0.1f)]
    private float mothDuration = 3f;

    [SerializeField, Min(0.1f)]
    private float signDuration = 2.5f;

    [SerializeField, Min(0f)]
    private float recoveryDuration = 0.75f;

    public float BirdDuration => birdDuration;
    public float MothDuration => mothDuration;
    public float SignDuration => signDuration;
    public float RecoveryDuration => recoveryDuration;

    private void OnValidate()
    {
        birdDuration = Mathf.Max(0.1f, birdDuration);
        mothDuration = Mathf.Max(0.1f, mothDuration);
        signDuration = Mathf.Max(0.1f, signDuration);
        recoveryDuration = Mathf.Max(
            0f,
            recoveryDuration
        );
    }
}
