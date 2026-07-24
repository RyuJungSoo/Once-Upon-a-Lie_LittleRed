using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MonsterHitFlash : MonoBehaviour
{
    private MonsterHealth monsterHealth;
    private SpriteRenderer spriteRenderer;
    private Coroutine flashRoutine;
    private Color originalColor;

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        originalColor = spriteRenderer.color;
        monsterHealth.HealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        monsterHealth.HealthChanged -= HandleHealthChanged;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        spriteRenderer.color = originalColor;
    }

    private void HandleHealthChanged(
        int previousHealth,
        int currentHealth
    )
    {
        if (currentHealth >= previousHealth)
        {
            return;
        }

        Flash();
    }

    private void Flash()
    {
        MonsterStats stats = monsterHealth.Stats;

        if (stats == null || stats.HitFlashDuration <= 0f)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        else
        {
            originalColor = spriteRenderer.color;
        }

        Color flashColor = stats.HitFlashColor;
        flashColor.a *= originalColor.a;
        spriteRenderer.color = flashColor;

        flashRoutine = StartCoroutine(
            RestoreColorAfterDelay(stats.HitFlashDuration)
        );
    }

    private IEnumerator RestoreColorAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }
}
