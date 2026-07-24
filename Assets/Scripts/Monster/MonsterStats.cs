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

    public int MaxHealth => maxHealth;
    public float Damage => damage;
    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public Color HitFlashColor => hitFlashColor;
    public float HitFlashDuration => hitFlashDuration;
    public float MoveSpeed => moveSpeed;

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        damage = Mathf.Max(0f, damage);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0.01f, knockbackDuration);
        hitFlashDuration = Mathf.Max(0f, hitFlashDuration);
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
}
