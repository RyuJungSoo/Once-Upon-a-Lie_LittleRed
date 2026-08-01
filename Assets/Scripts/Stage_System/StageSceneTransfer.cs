using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class StageSceneTransfer : MonoBehaviour
{
    [Header("Objects transferred to the next stage")]
    [SerializeField]
    private GameObject player;

    [SerializeField]
    private GameObject mainCamera;

    [SerializeField]
    private GameObject virtualCamera;

    private static readonly List<GameObject> TransferredRoots = new();
    private static bool transferInProgress;

    public void LoadNextStage()
    {
        if (transferInProgress)
        {
            return;
        }

        Scene currentScene = SceneManager.GetActiveScene();
        int nextSceneIndex = currentScene.buildIndex + 1;

        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                $"[{nameof(StageSceneTransfer)}] " +
                "다음 씬이 Build Settings에 없습니다. " +
                $"현재 Index: {currentScene.buildIndex}",
                this
            );
            return;
        }

        if (!TryPrepareTransfer())
        {
            return;
        }

        Time.timeScale = 1f;

        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartNextStage();
        }

        SceneManager.LoadScene(nextSceneIndex);
    }

    private bool TryPrepareTransfer()
    {
        if (player == null ||
            mainCamera == null ||
            virtualCamera == null)
        {
            Debug.LogError(
                $"[{nameof(StageSceneTransfer)}] " +
                "Player, Main Camera, CinemachineCamera 참조가 " +
                "모두 필요합니다.",
                this
            );
            return false;
        }

        TransferredRoots.Clear();
        TransferredRoots.AddRange(
            CollectTransferRoots(
                player,
                mainCamera,
                virtualCamera
            )
        );

        foreach (GameObject root in TransferredRoots)
        {
            DontDestroyOnLoad(root);
        }

        transferInProgress = true;
        SceneManager.sceneLoaded -= OnDestinationSceneLoaded;
        SceneManager.sceneLoaded += OnDestinationSceneLoaded;
        return true;
    }

    private static GameObject[] CollectTransferRoots(
        GameObject playerObject,
        GameObject mainCameraObject,
        GameObject virtualCameraObject
    )
    {
        HashSet<GameObject> roots = new();

        AddRoot(playerObject, roots);
        AddRoot(mainCameraObject, roots);
        AddRoot(virtualCameraObject, roots);

        GameObject[] result = new GameObject[roots.Count];
        roots.CopyTo(result);
        return result;
    }

    private static void AddRoot(
        GameObject target,
        HashSet<GameObject> roots
    )
    {
        if (target != null)
        {
            roots.Add(target.transform.root.gameObject);
        }
    }

    private static void OnDestinationSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode
    )
    {
        SceneManager.sceneLoaded -= OnDestinationSceneLoaded;

        foreach (GameObject root in TransferredRoots)
        {
            if (root != null)
            {
                SceneManager.MoveGameObjectToScene(root, scene);
            }
        }

        TransferredRoots.Clear();
        transferInProgress = false;
    }
}
