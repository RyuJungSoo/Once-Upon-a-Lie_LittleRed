using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonController : MonoBehaviour
{
    /// <summary>
    /// 별도의 게임 진행도 처리가 필요 없는
    /// 일반적인 씬 이동에 사용합니다.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning(
                $"[{nameof(SceneButtonController)}] " +
                "Scene name이 비어 있습니다.",
                this
            );

            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }


    /// <summary>
    /// 현재 씬을 단순히 다시 로드합니다.
    /// 게임오버 Retry 용도로는 사용하지 않습니다.
    /// </summary>
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.name
        );
    }


    /// <summary>
    /// 게임오버 후 전체 런을 초기화하고
    /// 첫 번째 스테이지부터 다시 시작합니다.
    /// </summary>
    public void RetryFromBeginning()
    {
        if (!GameManager.HasInstance)
        {
            Debug.LogWarning(
                $"[{nameof(SceneButtonController)}] " +
                "GameManager를 찾을 수 없습니다.",
                this
            );

            return;
        }

        GameManager.Instance
            .RetryFromBeginning();
    }


    /// <summary>
    /// 메인 메뉴로 돌아갑니다.
    /// 런 초기화는 GameManager가 처리합니다.
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (!GameManager.HasInstance)
        {
            Debug.LogWarning(
                $"[{nameof(SceneButtonController)}] " +
                "GameManager를 찾을 수 없습니다.",
                this
            );

            return;
        }

        GameManager.Instance
            .ReturnToMainMenu();
    }

    /// <summary>
    /// 현재 씬의 다음 Build Index 씬으로 이동합니다.
    /// 다음 씬이 존재하지 않으면 이동하지 않습니다.
    /// </summary>
    public void LoadNextScene()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        int nextSceneIndex =
            currentScene.buildIndex + 1;

        if (nextSceneIndex >=
            SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                $"[{nameof(SceneButtonController)}] " +
                "다음 씬이 Build Settings에 없습니다. " +
                $"현재 Index: {currentScene.buildIndex}",
                this
            );

            return;
        }

        Time.timeScale = 1f;

        /*
        * 다음 씬을 불러오기 전에
        * GameManager의 스테이지 인덱스를 증가시킵니다.
        */
        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartNextStage();
        }

        SceneManager.LoadScene(nextSceneIndex);
    }


    /// <summary>
    /// 게임을 종료합니다.
    /// </summary>
    public void QuitGame()
    {
        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication
            .isPlaying = false;
#endif
    }
}