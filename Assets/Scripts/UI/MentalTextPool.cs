using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MentalTextPool : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField]
    private MentalText mentalTextPrefab;

    [Tooltip("생성된 텍스트들이 배치될 부모입니다.")]
    [SerializeField]
    private Transform poolParent;

    [Header("Pool Settings")]
    [Tooltip("시작할 때 미리 생성할 개수입니다.")]
    [SerializeField, Min(1)]
    private int initialPoolSize = 3;

    [Tooltip("모든 텍스트가 사용 중일 때 추가 생성할지 여부입니다.")]
    [SerializeField]
    private bool canExpand = true;

    [Tooltip("확장 가능한 최대 텍스트 개수입니다.")]
    [SerializeField, Min(1)]
    private int maxPoolSize = 10;

    private readonly List<MentalText> pool =
        new List<MentalText>();

    public int PoolCount => pool.Count;

    private void Awake()
    {
        if (poolParent == null)
        {
            poolParent = transform;
        }

        maxPoolSize = Mathf.Max(
            initialPoolSize,
            maxPoolSize
        );

        InitializePool();
    }

    /// <summary>
    /// 비활성화된 MentalText를 반환합니다.
    /// 남은 객체가 없다면 설정에 따라 새 객체를 생성합니다.
    /// </summary>
    public bool TryGet(
        out MentalText mentalText
    )
    {
        RemoveDestroyedObjects();

        for (int i = 0; i < pool.Count; i++)
        {
            MentalText pooledText = pool[i];

            if (pooledText == null)
            {
                continue;
            }

            if (!pooledText.gameObject.activeSelf)
            {
                mentalText = pooledText;
                return true;
            }
        }

        if (canExpand &&
            pool.Count < maxPoolSize)
        {
            mentalText = CreateMentalText();
            return mentalText != null;
        }

        mentalText = null;
        return false;
    }

    /// <summary>
    /// 풀에 포함된 모든 텍스트를 비활성화합니다.
    /// </summary>
    public void DisableAll()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            MentalText mentalText = pool[i];

            if (mentalText == null)
            {
                continue;
            }

            mentalText.gameObject.SetActive(false);
        }
    }

    private void InitializePool()
    {
        if (mentalTextPrefab == null)
        {
            Debug.LogError(
                $"{nameof(MentalTextPool)}에 " +
                $"{nameof(mentalTextPrefab)}이 연결되지 않았습니다.",
                this
            );

            return;
        }

        for (int i = pool.Count;
             i < initialPoolSize;
             i++)
        {
            CreateMentalText();
        }
    }

    private MentalText CreateMentalText()
    {
        if (mentalTextPrefab == null)
        {
            return null;
        }

        MentalText newMentalText = Instantiate(
            mentalTextPrefab,
            poolParent,
            false
        );

        newMentalText.gameObject.SetActive(false);
        pool.Add(newMentalText);

        return newMentalText;
    }

    private void RemoveDestroyedObjects()
    {
        for (int i = pool.Count - 1;
             i >= 0;
             i--)
        {
            if (pool[i] == null)
            {
                pool.RemoveAt(i);
            }
        }
    }
}