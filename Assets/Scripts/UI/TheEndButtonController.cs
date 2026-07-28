using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class TheEndButtonController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField]
    private string targetSceneName = "MainMenu";

    [Header("Manager Cleanup")]
    [SerializeField]
    private string gameControllerTag = "GameController";

    [Header("Options")]
    [SerializeField]
    private bool loadSceneAsync = true;

    private Button button;
    private bool isTransitioning;

    private void Awake()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(
            HandleTheEndButtonClicked
        );
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleTheEndButtonClicked
            );
        }
    }

    public void HandleTheEndButtonClicked()
    {
        if (isTransitioning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError(
                $"[{nameof(TheEndButtonController)}] " +
                "이동할 씬 이름이 비어 있습니다.",
                this
            );

            return;
        }

        isTransitioning = true;
        button.interactable = false;

        StartCoroutine(
            CleanupAndLoadSceneRoutine()
        );
    }

    private IEnumerator CleanupAndLoadSceneRoutine()
    {
        DestroyGameControllerObjects();

        /*
         * Destroy는 호출 즉시 완전히 제거되는 것이 아니라
         * 현재 프레임이 끝날 때 반영됩니다.
         *
         * 한 프레임 기다린 후 씬을 불러와야
         * 기존 DontDestroyOnLoad 매니저가 남은 상태로
         * 메인 메뉴 초기화가 실행되는 문제를 줄일 수 있습니다.
         */
        yield return null;

        if (loadSceneAsync)
        {
            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    targetSceneName,
                    LoadSceneMode.Single
                );

            if (loadOperation == null)
            {
                Debug.LogError(
                    $"[{nameof(TheEndButtonController)}] " +
                    $"씬 로드를 시작하지 못했습니다: " +
                    targetSceneName,
                    this
                );

                isTransitioning = false;
                button.interactable = true;

                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(
                targetSceneName,
                LoadSceneMode.Single
            );
        }
    }

    private void DestroyGameControllerObjects()
    {
        GameObject[] controllerObjects;

        try
        {
            controllerObjects =
                GameObject.FindGameObjectsWithTag(
                    gameControllerTag
                );
        }
        catch (UnityException exception)
        {
            Debug.LogError(
                $"[{nameof(TheEndButtonController)}] " +
                $"태그를 찾을 수 없습니다: {gameControllerTag}\n" +
                exception.Message,
                this
            );

            return;
        }

        for (int i = 0;
             i < controllerObjects.Length;
             i++)
        {
            GameObject controllerObject =
                controllerObjects[i];

            if (controllerObject == null)
            {
                continue;
            }

            Destroy(controllerObject);
        }

        Debug.Log(
            $"[{nameof(TheEndButtonController)}] " +
            $"{gameControllerTag} 태그 오브젝트 " +
            $"{controllerObjects.Length}개 파괴 요청 완료.",
            this
        );
    }
}