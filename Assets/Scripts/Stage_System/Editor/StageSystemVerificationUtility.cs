using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class StageSystemVerificationUtility
{
    private static TestRunnerApi testRunner;
    private static VerificationCallbacks callbacks;

    [MenuItem("Tools/Stage System/Run Stage System Tests")]
    public static void RunStageSystemTests()
    {
        if (testRunner != null)
        {
            Debug.LogWarning(
                "[StageSystemTests] A test run is already active."
            );
            return;
        }

        testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
        callbacks = new VerificationCallbacks();
        testRunner.RegisterCallbacks(callbacks);

        ExecutionSettings settings = new(
            new Filter
            {
                testMode = TestMode.EditMode,
                testNames = new[]
                {
                    "StageSystemTests." +
                    "ChoosePrefab_UsesConfiguredWeights",
                    "StageSystemTests." +
                    "MonsterSpawner_DiscoversAddedAndRemovedSpawnPoints",
                    "StageSystemTests." +
                    "StageSceneTransfer_CollectsDistinctPlayerAndCameraRoots"
                }
            }
        )
        {
            runSynchronously = true
        };

        Debug.Log("[StageSystemTests] Starting 3 EditMode tests.");
        testRunner.Execute(settings);
    }

    private sealed class VerificationCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log(
                "[StageSystemTests] Finished: " +
                $"{result.PassCount} passed, " +
                $"{result.FailCount} failed, " +
                $"{result.SkipCount} skipped."
            );

            testRunner.UnregisterCallbacks(this);
            Object.DestroyImmediate(testRunner);
            testRunner = null;
            callbacks = null;
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.FailCount > 0)
            {
                Debug.LogError(
                    $"[StageSystemTests] {result.Name}: " +
                    result.Message
                );
            }
        }
    }
}
