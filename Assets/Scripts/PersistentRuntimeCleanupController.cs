using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PersistentRuntimeCleanupController
    : MonoBehaviour
{
    [Header("Objects To Destroy")]
    [SerializeField]
    [Tooltip(
        "MainMenu 진입 시 파괴할 " +
        "DontDestroyOnLoad 오브젝트를 등록합니다."
    )]
    private List<GameObject> objectsToDestroy = new();

    private bool isCleaningUp;


    /// <summary>
    /// 등록된 런타임 오브젝트만 파괴합니다.
    /// 이 컴포넌트와 GameManager 오브젝트는 유지합니다.
    /// </summary>
    public void CleanupRegisteredObjects()
    {
        if (isCleaningUp)
        {
            return;
        }

        isCleaningUp = true;

        HashSet<GameObject> processedObjects =
            new HashSet<GameObject>();

        foreach (GameObject target in objectsToDestroy)
        {
            if (target == null)
            {
                continue;
            }

            if (!processedObjects.Add(target))
            {
                continue;
            }

            // GameManager 오브젝트 자신은 유지
            if (target == gameObject)
            {
                Debug.LogWarning(
                    $"[{nameof(PersistentRuntimeCleanupController)}] " +
                    $"{target.name}은 GameManager 오브젝트이므로 " +
                    "파괴하지 않습니다.",
                    this
                );

                continue;
            }

            /*
             * GameManager의 부모를 파괴하면
             * GameManager도 함께 파괴되므로 제외합니다.
             */
            if (transform.IsChildOf(target.transform))
            {
                Debug.LogWarning(
                    $"[{nameof(PersistentRuntimeCleanupController)}] " +
                    $"{target.name}은 GameManager의 부모이므로 " +
                    "파괴하지 않습니다.",
                    target
                );

                continue;
            }

            /*
             * GameManager의 자식은 GameManager와 함께
             * 유지되어야 하므로 기본적으로 제외합니다.
             */
            if (target.transform.IsChildOf(transform))
            {
                Debug.LogWarning(
                    $"[{nameof(PersistentRuntimeCleanupController)}] " +
                    $"{target.name}은 GameManager의 자식이므로 " +
                    "파괴하지 않습니다.",
                    target
                );

                continue;
            }

            Destroy(target);
        }

        /*
         * GameManager가 계속 살아 있고 다음 게임에서도
         * 다시 정리할 수 있어야 하므로 잠금 상태를 해제합니다.
         */
        isCleaningUp = false;
    }


    public void RegisterObject(GameObject target)
    {
        if (target == null ||
            target == gameObject ||
            objectsToDestroy.Contains(target))
        {
            return;
        }

        objectsToDestroy.Add(target);
    }


    public void UnregisterObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        objectsToDestroy.Remove(target);
    }
}