using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PersistentRuntimeCleanupController
    : MonoBehaviour
{
    [Header("Objects To Destroy")]
    [SerializeField]
    [Tooltip(
        "MainMenu 진입 시 먼저 파괴할 " +
        "DontDestroyOnLoad 오브젝트를 등록합니다."
    )]
    private List<GameObject> objectsToDestroy = new();

    private bool isCleaningUp;


    /// <summary>
    /// Inspector에 등록된 오브젝트를 먼저 파괴하고,
    /// 마지막으로 이 컴포넌트가 붙은 오브젝트를 파괴합니다.
    /// </summary>
    public void CleanupAndDestroySelf()
    {
        if (isCleaningUp)
        {
            return;
        }

        isCleaningUp = true;

        DestroyRegisteredObjects();

        /*
         * PersistentRuntimeCleanupController가
         * GameManager 오브젝트에 붙어 있으므로
         * GameManager도 마지막에 함께 파괴됩니다.
         */
        Destroy(gameObject);
    }


    private void DestroyRegisteredObjects()
    {
        if (objectsToDestroy == null ||
            objectsToDestroy.Count == 0)
        {
            return;
        }

        HashSet<GameObject> processedObjects =
            new HashSet<GameObject>();

        foreach (GameObject target in objectsToDestroy)
        {
            if (target == null)
            {
                continue;
            }

            // 중복으로 등록된 오브젝트는 한 번만 처리
            if (!processedObjects.Add(target))
            {
                continue;
            }

            // 자기 자신은 마지막에 파괴
            if (target == gameObject)
            {
                continue;
            }

            /*
             * GameManager의 부모를 먼저 파괴하면
             * GameManager도 함께 제거되므로 목록에서 제외합니다.
             */
            if (transform.IsChildOf(target.transform))
            {
                Debug.LogWarning(
                    $"[{nameof(PersistentRuntimeCleanupController)}] " +
                    $"{target.name}은 GameManager의 부모이므로 " +
                    "정리 목록에서 제외합니다.",
                    target
                );

                continue;
            }

            Destroy(target);
        }
    }
}