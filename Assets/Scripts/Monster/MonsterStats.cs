using UnityEngine;

[CreateAssetMenu(fileName = "MonsterStats", menuName = "Once Upon a Lie/Monster Stats")]
public sealed class MonsterStats : ScriptableObject
{
    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 1;
    [SerializeField, Min(0f)] private float damage = 10f;

    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackDistance = 0.3f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.1f;

    [Header("Hit Feedback")]
    [SerializeField] private Color hitFlashColor =
        new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2f;

    [Header("Experience Crystal Drop")]
    [SerializeField]
    private GameObject experienceCrystalDropPrefab;

    [Header("Recovery Item Drops")]
    [Tooltip("RedBerry prefab dropped by this monster.")]
    [SerializeField]
    private GameObject redBerryDropPrefab;

    [Tooltip("Independent RedBerry drop chance in percent.")]
    [SerializeField, Range(0f, 100f)]
    private float redBerryDropChancePercent = 10f;

    [Tooltip("StarCandy prefab dropped by this monster.")]
    [SerializeField]
    private GameObject starCandyDropPrefab;

    [Tooltip("Independent StarCandy drop chance in percent.")]
    [SerializeField, Range(0f, 100f)]
    private float starCandyDropChancePercent = 20f;

    [Tooltip("Pie prefab dropped by this monster.")]
    [SerializeField]
    private GameObject pieDropPrefab;

    [Tooltip("Independent Pie drop chance in percent.")]
    [SerializeField, Range(0f, 100f)]
    private float pieDropChancePercent = 3f;

    public int MaxHealth => maxHealth;
    public float Damage => damage;
    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public Color HitFlashColor => hitFlashColor;
    public float HitFlashDuration => hitFlashDuration;
    public float MoveSpeed => moveSpeed;
    public GameObject ExperienceCrystalDropPrefab =>
        experienceCrystalDropPrefab;
    public GameObject RedBerryDropPrefab =>
        redBerryDropPrefab;
    public float RedBerryDropChancePercent =>
        redBerryDropChancePercent;
    public GameObject StarCandyDropPrefab =>
        starCandyDropPrefab;
    public float StarCandyDropChancePercent =>
        starCandyDropChancePercent;
    public GameObject PieDropPrefab =>
        pieDropPrefab;
    public float PieDropChancePercent =>
        pieDropChancePercent;

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        damage = Mathf.Max(0f, damage);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0.01f, knockbackDuration);
        hitFlashDuration = Mathf.Max(0f, hitFlashDuration);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        redBerryDropChancePercent = Mathf.Clamp(
            redBerryDropChancePercent,
            0f,
            100f
        );
        starCandyDropChancePercent = Mathf.Clamp(
            starCandyDropChancePercent,
            0f,
            100f
        );
        pieDropChancePercent = Mathf.Clamp(
            pieDropChancePercent,
            0f,
            100f
        );
    }
}
