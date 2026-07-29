using UnityEngine;

[CreateAssetMenu(
    fileName = "GrandmaBossProfile",
    menuName = "Once Upon a Lie/Boss/Grandma Profile"
)]
public sealed class GrandmaBossProfile : ScriptableObject
{
    [Header("Pattern Timing")]
    [SerializeField, Min(0.1f)]
    private float teaCupDuration = 3f;

    [SerializeField, Min(0.1f)]
    private float blanketDuration = 2.5f;

    [SerializeField, Min(0.1f)]
    private float redStringDuration = 2.5f;

    [SerializeField, Min(0f)]
    private float recoveryDuration = 0.75f;

    [Header("Blanket Restraint")]
    [SerializeField, Min(0f)]
    private float restraintDuration = 1.5f;

    public float TeaCupDuration => teaCupDuration;
    public float BlanketDuration => blanketDuration;
    public float RedStringDuration => redStringDuration;
    public float RecoveryDuration => recoveryDuration;
    public float RestraintDuration => restraintDuration;

    private void OnValidate()
    {
        teaCupDuration = Mathf.Max(0.1f, teaCupDuration);
        blanketDuration = Mathf.Max(
            0.1f,
            blanketDuration
        );
        redStringDuration = Mathf.Max(
            0.1f,
            redStringDuration
        );
        recoveryDuration = Mathf.Max(
            0f,
            recoveryDuration
        );
        restraintDuration = Mathf.Max(
            0f,
            restraintDuration
        );
    }
}
