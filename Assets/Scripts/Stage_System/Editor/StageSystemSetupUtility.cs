using UnityEditor;
using UnityEngine;

public static class StageSystemSetupUtility
{
    private static readonly Vector3[] SpawnPositions =
    {
        new(-12f, 0f, 0f),
        new(12f, 0f, 0f),
        new(0f, 7f, 0f),
        new(0f, -7f, 0f),
        new(-10f, 6f, 0f),
        new(10f, 6f, 0f),
        new(-10f, -6f, 0f),
        new(10f, -6f, 0f)
    };

    [MenuItem("GameObject/Stage System", false, 10)]
    public static void Create(MenuCommand menuCommand)
    {
        GameObject systemObject = new("StageSystem");
        Undo.RegisterCreatedObjectUndo(systemObject, "Create Stage System");
        GameObjectUtility.SetParentAndAlign(
            systemObject,
            menuCommand.context as GameObject
        );
        systemObject.AddComponent<StageDirector>();

        for (int index = 0; index < SpawnPositions.Length; index++)
        {
            GameObject point = new($"SpawnPoint{index + 1}");
            Undo.RegisterCreatedObjectUndo(point, "Create Spawn Point");
            point.transform.SetParent(systemObject.transform);
            point.transform.localPosition = SpawnPositions[index];
            point.AddComponent<MonsterSpawnPoint>();
        }

        Selection.activeGameObject = systemObject;
    }
}
