using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class StageUiInputSceneOwnershipTests
{
    private static readonly string[] BuildSceneNames =
    {
        "MainMenu",
        "Stage1_Scene",
        "Stage2_Scene",
        "Stage3_Scene",
        "Ending"
    };

    [UnityTest]
    public IEnumerator EveryBuildSceneOwnsOneActiveEventSystem()
    {
        foreach (string sceneName in BuildSceneNames)
        {
            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(sceneName);

            yield return loadOperation;

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

        Type stageSceneTransferType =
            Type.GetType("StageSceneTransfer, Assembly-CSharp");

        Assert.That(stageSceneTransferType, Is.Not.Null);

        Component stageSceneTransfer =
            Object.FindFirstObjectByType(
                stageSceneTransferType,
                FindObjectsInactive.Include
            ) as Component;

        Assert.That(stageSceneTransfer, Is.Not.Null);

        MethodInfo loadNextStage =
            stageSceneTransferType.GetMethod(
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
}
