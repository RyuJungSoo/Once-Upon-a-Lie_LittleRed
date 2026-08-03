using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
public sealed class MonsterKnockback : MonoBehaviour
{
    private MonsterHealth monsterHealth;
    private Coroutine knockbackRoutine;
    public bool IsActive => knockbackRoutine != null;

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
    }

    private void OnDisable()
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }
    }

    public void Apply(Vector2 direction)
    {
        MonsterStats stats = monsterHealth.Stats;

        if (stats == null ||
            stats.KnockbackDistance <= 0f ||
            direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(
            MoveBackward(
                direction.normalized,
                stats.KnockbackDistance,
                stats.KnockbackDuration
            )
        );
    }

    private IEnumerator MoveBackward(
        Vector2 direction,
        float distance,
        float duration
    )
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition =
            startPosition + (Vector3)(direction * distance);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            float easedProgress =
                1f - Mathf.Pow(1f - progress, 3f);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                easedProgress
            );

            yield return null;
        }

        transform.position = targetPosition;
        knockbackRoutine = null;
    }
}
