using UnityEngine;

[CreateAssetMenu(
    fileName = "DeerKingBossProfile",
    menuName = "Once Upon a Lie/Boss/Deer King Profile"
)]
public sealed class DeerKingBossProfile : ScriptableObject
{
    [Header("Pattern Timing")]
    [SerializeField, Min(0.1f)]
    private float ramDuration = 2.5f;

    [SerializeField, Min(0.1f)]
    private float rangedDuration = 3f;

    [SerializeField, Min(0f)]
    private float recoveryDuration = 0.75f;

    [Header("Aimed Charge")]
    [SerializeField]
    private MonsterAimedChargeSettings aimedCharge =
        new MonsterAimedChargeSettings();

    public float RamDuration => ramDuration;
    public float RangedDuration => rangedDuration;
    public float RecoveryDuration => recoveryDuration;
    public MonsterAimedChargeSettings AimedCharge =>
        aimedCharge;

    private void OnEnable()
    {
        EnsureSettings();
    }

    private void OnValidate()
    {
        EnsureSettings();

        ramDuration = Mathf.Max(0.1f, ramDuration);
        rangedDuration = Mathf.Max(0.1f, rangedDuration);
        recoveryDuration = Mathf.Max(
            0f,
            recoveryDuration
        );
        aimedCharge.Validate();
    }

    private void EnsureSettings()
    {
        if (aimedCharge == null)
        {
            aimedCharge =
                new MonsterAimedChargeSettings();
        }
    }
}
