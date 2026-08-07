using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class PlayerSetBootstrapper
{
    private const string Stage1SceneName = "Stage1_Scene";
    private const string PlayerSetSceneName = "Stage2_Scene";
    private const string Stage3SceneName = "Stage3_Scene";

    private static BootstrapRunner runner;

    private static bool IsBootstrapping { get; set; }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        IsBootstrapping = false;
        runner = null;
    }

    internal static PlayerMovement FindRuntimePlayer(
        Scene destinationScene
    )
    {
        PlayerMovement[] players =
            Object.FindObjectsByType<PlayerMovement>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
        PlayerMovement fallback = null;

        foreach (PlayerMovement player in players)
        {
            fallback ??= player;

            if (player.gameObject.scene != destinationScene)
            {
                return player;
            }
        }

        return fallback;
    }

    private static void OnSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode
    )
    {
        if (!IsStageScene(scene.name))
        {
            return;
        }

        if (scene.name == PlayerSetSceneName)
        {
            RemoveDuplicatePlayerSet(scene);
        }

        if (IsBootstrapping)
        {
            return;
        }

        PlayerMovement player = FindRuntimePlayer(scene);

        if (player != null)
        {
            PlayerSpawnPoint.TrySpawn(player.transform, scene);
            return;
        }

        GameObject runnerObject =
            new(nameof(PlayerSetBootstrapper));
        runner = runnerObject.AddComponent<BootstrapRunner>();
        Object.DontDestroyOnLoad(runnerObject);
        runner.Begin(scene);
    }

    private static void RemoveDuplicatePlayerSet(Scene scene)
    {
        PlayerMovement runtimePlayer = FindRuntimePlayer(scene);

        if (runtimePlayer == null ||
            runtimePlayer.gameObject.scene == scene)
        {
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<PlayerMovement>(true) != null)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
        }
    }

    internal static bool IsStageScene(string sceneName)
    {
        return sceneName == Stage1SceneName ||
               sceneName == PlayerSetSceneName ||
               sceneName == Stage3SceneName;
    }

    private sealed class BootstrapRunner : MonoBehaviour
    {
        private readonly List<GameObject> disabledEventSystems = new();
        private Scene destinationScene;

        internal void Begin(Scene scene)
        {
            destinationScene = scene;
            StartCoroutine(Bootstrap());
        }

        private IEnumerator Bootstrap()
        {
            IsBootstrapping = true;
            SetDestinationEventSystemsActive(false);

            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    PlayerSetSceneName,
                    LoadSceneMode.Additive
                );
            yield return loadOperation;

            Scene playerSetScene =
                SceneManager.GetSceneByName(PlayerSetSceneName);
            PlayerMovement player =
                FindPlayerInScene(playerSetScene);

            if (player == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerSetBootstrapper)}] " +
                    $"{PlayerSetSceneName}에서 PlayerSet의 Player를 " +
                    "찾을 수 없습니다."
                );
                Finish();
                yield break;
            }

            Object.DontDestroyOnLoad(
                player.transform.root.gameObject
            );

            if (destinationScene.IsValid() &&
                destinationScene.isLoaded)
            {
                SceneManager.SetActiveScene(destinationScene);
            }

            AsyncOperation unloadOperation =
                SceneManager.UnloadSceneAsync(playerSetScene);

            if (unloadOperation != null)
            {
                yield return unloadOperation;
            }

            IsBootstrapping = false;
            SetDestinationEventSystemsActive(true);
            PlayerSpawnPoint.TrySpawn(
                player.transform,
                destinationScene
            );
            Finish();
        }

        private static PlayerMovement FindPlayerInScene(
            Scene scene
        )
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                PlayerMovement player =
                    root.GetComponentInChildren<PlayerMovement>(true);

                if (player != null)
                {
                    return player;
                }
            }

            return null;
        }

        private void SetDestinationEventSystemsActive(bool active)
        {
            if (active)
            {
                foreach (GameObject eventSystem in disabledEventSystems)
                {
                    if (eventSystem != null)
                    {
                        eventSystem.SetActive(true);
                    }
                }

                disabledEventSystems.Clear();
                return;
            }

            EventSystem[] eventSystems =
                Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            foreach (EventSystem eventSystem in eventSystems)
            {
                if (eventSystem.gameObject.scene != destinationScene)
                {
                    continue;
                }

                disabledEventSystems.Add(eventSystem.gameObject);
                eventSystem.gameObject.SetActive(false);
            }
        }

        private void Finish()
        {
            SetDestinationEventSystemsActive(true);
            IsBootstrapping = false;
            runner = null;
            Destroy(gameObject);
        }
    }
}
