using UnityEngine;

[CreateAssetMenu(fileName = "MonsterStats", menuName = "Once Upon a Lie/Monster Stats")]
public sealed class MonsterStats : ScriptableObject
{
    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 1;
    [SerializeField, Min(0f)] private float damage = 10f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2f;

    public int MaxHealth => maxHealth;
    public float Damage => damage;
    public float MoveSpeed => moveSpeed;

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        damage = Mathf.Max(0f, damage);
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
}
