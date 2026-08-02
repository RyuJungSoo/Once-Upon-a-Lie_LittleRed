using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class StageUiInputPersistenceTests
{
    private static readonly Type StageSceneTransferType =
        Type.GetType("StageSceneTransfer, Assembly-CSharp");

    [Test]
    public async Task StageTransitionPreservesEventSystemForPersistentUI()
    {
        TaskCompletionSource<Scene> sourceLoaded =
            new TaskCompletionSource<Scene>();

        void HandleSourceSceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode
        )
        {
            if (scene.name == "Stage1_Scene")
            {
                sourceLoaded.TrySetResult(scene);
            }
        }

        SceneManager.sceneLoaded += HandleSourceSceneLoaded;

        try
        {
            SceneManager.LoadScene("Stage1_Scene");

            Task completedTask = await Task.WhenAny(
                sourceLoaded.Task,
                Task.Delay(TimeSpan.FromSeconds(10))
            );

            Assert.That(
                completedTask,
                Is.SameAs(sourceLoaded.Task),
                "Stage1 did not finish loading within 10 seconds."
            );

            await sourceLoaded.Task;
        }
        finally
        {
            SceneManager.sceneLoaded -= HandleSourceSceneLoaded;
        }

        EventSystem sourceEventSystem =
            UnityEngine.Object.FindFirstObjectByType<EventSystem>(
                FindObjectsInactive.Include
            );

        Assert.That(sourceEventSystem, Is.Not.Null);
        Assert.That(StageSceneTransferType, Is.Not.Null);

        Component stageSceneTransfer =
            UnityEngine.Object.FindFirstObjectByType(
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

        TaskCompletionSource<Scene> destinationLoaded =
            new TaskCompletionSource<Scene>();

        void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode
        )
        {
            if (scene.name == "Stage2_Scene")
            {
                destinationLoaded.TrySetResult(scene);
            }
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;

        try
        {
            loadNextStage.Invoke(stageSceneTransfer, null);

            Task completedTask = await Task.WhenAny(
                destinationLoaded.Task,
                Task.Delay(TimeSpan.FromSeconds(10))
            );

            Assert.That(
                completedTask,
                Is.SameAs(destinationLoaded.Task),
                "Stage2 did not finish loading within 10 seconds."
            );

            await destinationLoaded.Task;
        }
        finally
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        EventSystem destinationEventSystem =
            UnityEngine.Object.FindFirstObjectByType<EventSystem>(
                FindObjectsInactive.Include
            );

        Assert.That(
            destinationEventSystem,
            Is.SameAs(sourceEventSystem),
            "Stage2 must retain the EventSystem used by the persistent UI."
        );

        TaskCompletionSource<Scene> stageReloaded =
            new TaskCompletionSource<Scene>();

        void HandleStageReloaded(
            Scene scene,
            LoadSceneMode loadSceneMode
        )
        {
            if (scene.name == "Stage2_Scene")
            {
                stageReloaded.TrySetResult(scene);
            }
        }

        SceneManager.sceneLoaded += HandleStageReloaded;

        try
        {
            SceneManager.LoadScene("Stage2_Scene");

            Task completedTask = await Task.WhenAny(
                stageReloaded.Task,
                Task.Delay(TimeSpan.FromSeconds(10))
            );

            Assert.That(
                completedTask,
                Is.SameAs(stageReloaded.Task),
                "Stage2 did not reload within 10 seconds."
            );

            await stageReloaded.Task;
        }
        finally
        {
            SceneManager.sceneLoaded -= HandleStageReloaded;
        }

        EventSystem reloadedEventSystem =
            UnityEngine.Object.FindFirstObjectByType<EventSystem>(
                FindObjectsInactive.Include
            );

        Assert.That(
            reloadedEventSystem,
            Is.SameAs(sourceEventSystem),
            "Reloading Stage2 must retain the persistent UI EventSystem."
        );
    }
}
