using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class StageUiInputSceneOwnershipTests
{
    private static readonly Type PlayerMovementType =
        Type.GetType("PlayerMovement, Assembly-CSharp");

    private static readonly Type PlayerSpawnPointType =
        Type.GetType("PlayerSpawnPoint, Assembly-CSharp");

    private static readonly Type StageSceneTransferType =
        Type.GetType("StageSceneTransfer, Assembly-CSharp");

    private static readonly string[] BuildSceneNames =
    {
        "MainMenu",
        "Stage1_Scene",
        "Stage2_Scene",
        "Stage3_Scene",
        "Ending"
    };

    [Test]
    [Timeout(30000)]
    public async Task EveryBuildSceneOwnsOneActiveEventSystem()
    {
        foreach (string sceneName in BuildSceneNames)
        {
            if (sceneName.StartsWith(
                    "Stage",
                    StringComparison.Ordinal))
            {
                await LoadScene(
                    sceneName,
                    () => SceneManager.LoadScene(sceneName)
                );
            }
            else
            {
                await LoadSceneWithoutPlayer(sceneName);
            }

            EventSystem[] eventSystems =
                Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            Assert.That(
                eventSystems,
                Has.Length.EqualTo(1),
                $"{sceneName} must contain exactly one active EventSystem."
            );
            Assert.That(
                eventSystems[0].gameObject.scene.name,
                Is.EqualTo(sceneName),
                $"{sceneName} must own its EventSystem."
            );
        }
    }

    private static async Task LoadSceneWithoutPlayer(
        string sceneName
    )
    {
        TaskCompletionSource<Scene> sceneLoaded = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode
        )
        {
            if (scene.name == sceneName)
            {
                sceneLoaded.TrySetResult(scene);
            }
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;

        try
        {
            SceneManager.LoadScene(sceneName);

            Task completed = await Task.WhenAny(
                sceneLoaded.Task,
                Task.Delay(TimeSpan.FromSeconds(10))
            );

            Assert.That(
                completed,
                Is.SameAs(sceneLoaded.Task),
                $"{sceneName} did not finish loading within 10 seconds."
            );
        }
        finally
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    [Test]
    [Timeout(10000)]
    public async Task SceneTransitionReplacesEventSystem()
    {
        TaskCompletionSource<Scene> sourceLoaded = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        void HandleSourceLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode
        )
        {
            if (scene.name == "Stage1_Scene")
            {
                sourceLoaded.TrySetResult(scene);
            }
        }

        SceneManager.sceneLoaded += HandleSourceLoaded;

        try
        {
            SceneManager.LoadScene("Stage1_Scene");
            await sourceLoaded.Task;
        }
        finally
        {
            SceneManager.sceneLoaded -= HandleSourceLoaded;
        }

        EventSystem sourceEventSystem =
            Object.FindFirstObjectByType<EventSystem>();

        Assert.That(sourceEventSystem, Is.Not.Null);

        Assert.That(StageSceneTransferType, Is.Not.Null);

        Component stageSceneTransfer =
            Object.FindFirstObjectByType(
                StageSceneTransferType,
                FindObjectsInactive.Include
            ) as Component;

        Assert.That(stageSceneTransfer, Is.Not.Null);

        MethodInfo loadNextStage =
            StageSceneTransferType.GetMethod(
                "LoadNextStage",
                BindingFlags.Instance |
                BindingFlags.Public
            );

        Assert.That(loadNextStage, Is.Not.Null);

        TaskCompletionSource<Scene> destinationLoaded = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        void HandleDestinationLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode
        )
        {
            if (scene.name == "Stage2_Scene")
            {
                destinationLoaded.TrySetResult(scene);
            }
        }

        SceneManager.sceneLoaded += HandleDestinationLoaded;

        try
        {
            loadNextStage.Invoke(stageSceneTransfer, null);
            await destinationLoaded.Task;
        }
        finally
        {
            SceneManager.sceneLoaded -= HandleDestinationLoaded;
        }

        EventSystem[] destinationEventSystems =
            Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        Assert.That(
            destinationEventSystems,
            Has.Length.EqualTo(1),
            "PlayerSet transfer must not carry Stage1's EventSystem."
        );
        Assert.That(
            destinationEventSystems[0].gameObject.scene.name,
            Is.EqualTo("Stage2_Scene")
        );
        Assert.That(
            destinationEventSystems[0],
            Is.Not.SameAs(sourceEventSystem),
            "Stage2 must use its own EventSystem."
        );
    }

    [Test]
    [Timeout(30000)]
    public async Task StageTransitionsSpawnPlayerAtEachSceneSpawnPoint()
    {
        Assert.That(PlayerMovementType, Is.Not.Null);
        Assert.That(PlayerSpawnPointType, Is.Not.Null);
        Assert.That(StageSceneTransferType, Is.Not.Null);

        await LoadScene("Stage1_Scene", () =>
            SceneManager.LoadScene("Stage1_Scene")
        );
        AssertPlayerAtSpawnPoint("Stage1_Scene");

        await LoadNextStage("Stage2_Scene");
        AssertPlayerAtSpawnPoint("Stage2_Scene");

        await LoadNextStage("Stage3_Scene");
        AssertPlayerAtSpawnPoint("Stage3_Scene");
    }

    private static async Task LoadNextStage(
        string destinationSceneName
    )
    {
        Component stageSceneTransfer =
            Object.FindFirstObjectByType(
                StageSceneTransferType,
                FindObjectsInactive.Include
            ) as Component;

        Assert.That(
            stageSceneTransfer,
            Is.Not.Null,
            $"{SceneManager.GetActiveScene().name} needs StageSceneTransfer."
        );

        MethodInfo loadNextStage =
            StageSceneTransferType.GetMethod(
                "LoadNextStage",
                BindingFlags.Instance |
                BindingFlags.Public
            );

        Assert.That(loadNextStage, Is.Not.Null);

        await LoadScene(
            destinationSceneName,
            () => loadNextStage.Invoke(
                stageSceneTransfer,
                null
            )
        );
    }

    private static async Task LoadScene(
        string sceneName,
        Action triggerLoad
    )
    {
        EventInfo playerSpawnedEvent =
            PlayerSpawnPointType.GetEvent(
                "PlayerSpawned",
                BindingFlags.Static |
                BindingFlags.Public
            );

        Assert.That(
            playerSpawnedEvent,
            Is.Not.Null,
            "PlayerSpawnPoint must publish completed player spawns."
        );

        TaskCompletionSource<Scene> playerSpawned = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        void HandlePlayerSpawned(Scene scene)
        {
            if (scene.name == sceneName)
            {
                playerSpawned.TrySetResult(scene);
            }
        }

        Action<Scene> handler = HandlePlayerSpawned;
        playerSpawnedEvent.AddEventHandler(null, handler);

        try
        {
            triggerLoad();

            Task completed = await Task.WhenAny(
                playerSpawned.Task,
                Task.Delay(TimeSpan.FromSeconds(10))
            );

            Assert.That(
                completed,
                Is.SameAs(playerSpawned.Task),
                $"{sceneName} did not spawn Player within 10 seconds."
            );
        }
        finally
        {
            playerSpawnedEvent.RemoveEventHandler(
                null,
                handler
            );
        }
    }

    private static void AssertPlayerAtSpawnPoint(
        string sceneName
    )
    {
        Component player =
            Object.FindFirstObjectByType(
                PlayerMovementType,
                FindObjectsInactive.Include
            ) as Component;
        Component spawnPoint =
            Object.FindFirstObjectByType(
                PlayerSpawnPointType,
                FindObjectsInactive.Include
            ) as Component;

        Assert.That(player, Is.Not.Null);
        Assert.That(spawnPoint, Is.Not.Null);
        Assert.That(
            spawnPoint.gameObject.scene.name,
            Is.EqualTo(sceneName)
        );
        Assert.That(
            Vector3.Distance(
                player.transform.position,
                spawnPoint.transform.position
            ),
            Is.LessThan(0.001f)
        );
        Assert.That(
            Quaternion.Angle(
                player.transform.rotation,
                spawnPoint.transform.rotation
            ),
            Is.LessThan(0.001f)
        );
    }
}
