using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class StageSceneTransfer : MonoBehaviour
{
    private const string CameraBoundsTag = "CameraBounds";

    [Header("Objects transferred to the next stage")]
    [SerializeField]
    private GameObject player;

    [SerializeField]
    private GameObject mainCamera;

    [SerializeField]
    private GameObject virtualCamera;

    private static readonly List<GameObject> TransferredRoots = new();

    /*
     * OnDestinationSceneLoaded()는 static 함수이므로,
     * 다음 씬에서 Confiner를 다시 설정할 수 있도록
     * 전송할 Virtual Camera 참조를 임시로 보관합니다.
     */
    private static GameObject transferredVirtualCamera;
    private static AudioListener transferredAudioListener;

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
        // Inspector 참조가 비어 있으면 현재 씬에서 다시 찾습니다.
        ResolveMissingReferences();

        if (player == null ||
            mainCamera == null ||
            virtualCamera == null)
        {
            Debug.LogError(
                $"[{nameof(StageSceneTransfer)}] " +
                "Player, Main Camera, CinemachineCamera 참조가 " +
                "모두 필요합니다.\n" +
                $"Player: {(player != null ? "OK" : "Missing")}\n" +
                $"Main Camera: {(mainCamera != null ? "OK" : "Missing")}\n" +
                $"CinemachineCamera: {(virtualCamera != null ? "OK" : "Missing")}",
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

        // 다음 씬에서 CameraBounds를 다시 연결할 때 사용합니다.
        transferredVirtualCamera = virtualCamera;
        transferredAudioListener =
            mainCamera.GetComponent<AudioListener>();

        if (transferredAudioListener != null)
        {
            transferredAudioListener.enabled = false;
        }

        foreach (GameObject root in TransferredRoots)
        {
            DontDestroyOnLoad(root);
        }

        transferInProgress = true;
        SceneManager.sceneLoaded -= OnDestinationSceneLoaded;
        SceneManager.sceneLoaded += OnDestinationSceneLoaded;
        return true;
    }

    /// <summary>
    /// 비어 있는 Player, Main Camera, Virtual Camera 참조를
    /// 현재 씬에서 다시 찾습니다.
    /// </summary>
    private void ResolveMissingReferences()
    {
        if (player == null)
        {
            PlayerMovement playerMovement =
                FindFirstObjectByType<PlayerMovement>();

            if (playerMovement != null)
            {
                player = playerMovement.gameObject;
            }
        }

        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.gameObject;
        }

        if (virtualCamera == null)
        {
            CinemachineCamera foundVirtualCamera =
                FindFirstObjectByType<CinemachineCamera>();

            if (foundVirtualCamera != null)
            {
                virtualCamera = foundVirtualCamera.gameObject;
            }
        }
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

        if (transferredAudioListener != null)
        {
            transferredAudioListener.enabled = true;
        }

        /*
         * 전송된 Cinemachine Camera에 새 씬의
         * CameraBounds를 다시 연결합니다.
         * CameraBounds가 없는 씬은 오류 없이 처리합니다.
         */
        RefreshCameraBounds();

        TransferredRoots.Clear();
        transferredVirtualCamera = null;
        transferredAudioListener = null;
        transferInProgress = false;
    }

    /// <summary>
    /// 새로 로드된 씬의 CameraBounds를 찾아
    /// 전송된 Cinemachine Confiner 2D에 연결합니다.
    /// </summary>
    private static void RefreshCameraBounds()
    {
        if (transferredVirtualCamera == null)
        {
            Debug.LogWarning(
                $"[{nameof(StageSceneTransfer)}] " +
                "전송된 CinemachineCamera 참조가 없습니다."
            );
            return;
        }

        CinemachineConfiner2D confiner =
            transferredVirtualCamera.GetComponent<CinemachineConfiner2D>();

        if (confiner == null)
        {
            confiner = transferredVirtualCamera
                .GetComponentInChildren<CinemachineConfiner2D>(true);
        }

        if (confiner == null)
        {
            Debug.LogWarning(
                $"[{nameof(StageSceneTransfer)}] " +
                $"{transferredVirtualCamera.name}에서 " +
                "CinemachineConfiner2D를 찾을 수 없습니다.",
                transferredVirtualCamera
            );
            return;
        }

        GameObject cameraBoundsObject;

        try
        {
            cameraBoundsObject =
                GameObject.FindWithTag(CameraBoundsTag);
        }
        catch (UnityException exception)
        {
            confiner.BoundingShape2D = null;
            confiner.InvalidateBoundingShapeCache();

            Debug.LogWarning(
                $"[{nameof(StageSceneTransfer)}] " +
                $"{CameraBoundsTag} 태그가 등록되어 있지 않습니다.\n" +
                exception.Message
            );
            return;
        }

        // 현재 씬에 CameraBounds가 없으면 Confiner를 비웁니다.
        if (cameraBoundsObject == null)
        {
            confiner.BoundingShape2D = null;
            confiner.InvalidateBoundingShapeCache();
            return;
        }

        if (!cameraBoundsObject.TryGetComponent(
                out Collider2D cameraBounds))
        {
            confiner.BoundingShape2D = null;
            confiner.InvalidateBoundingShapeCache();

            Debug.LogWarning(
                $"[{nameof(StageSceneTransfer)}] " +
                $"{cameraBoundsObject.name}에 Collider2D가 없습니다.",
                cameraBoundsObject
            );
            return;
        }

        confiner.BoundingShape2D = cameraBounds;

        // 이전 씬의 경계 캐시를 제거합니다.
        confiner.InvalidateBoundingShapeCache();
    }
}
