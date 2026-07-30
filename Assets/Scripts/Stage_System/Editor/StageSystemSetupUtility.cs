using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StageSystemSetupUtility
{
    private const string ConfigFolder =
        "Assets/Scenes/Stage_System";
    private const string FirstConfigPath =
        ConfigFolder + "/Stage1Definition.asset";

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

    [InitializeOnLoadMethod]
    private static void InstallDefaultsOnce()
    {
        if (!File.Exists(FirstConfigPath))
        {
            EditorApplication.delayCall += Install;
        }
    }

    [MenuItem("Tools/Stage System/Create Or Repair Default Setup")]
    public static void Install()
    {
        if (
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling
        )
        {
            return;
        }

        EnsureFolder(ConfigFolder);
        Dictionary<string, GameObject> monsters = LoadMonsterPrefabs();

        StageDefinition stage1 = CreateDefinitionIfMissing(
            FirstConfigPath,
            "Stage 1",
            false,
            "Stage2_Scene",
            CreateStage1Waves(monsters)
        );
        StageDefinition stage2 = CreateDefinitionIfMissing(
            ConfigFolder + "/Stage2Definition.asset",
            "Stage 2",
            false,
            "Stage3_Scene",
            CreateStage2Waves(monsters)
        );
        StageDefinition stage3 = CreateDefinitionIfMissing(
            ConfigFolder + "/Stage3Definition.asset",
            "Stage 3",
            true,
            "Ending",
            CreateStage3Waves(monsters)
        );

        AssetDatabase.SaveAssets();

        InstallScene(
            "Assets/Scenes/Stage1_Scene.unity",
            stage1
        );
        InstallScene(
            "Assets/Scenes/Stage2_Scene.unity",
            stage2
        );
        InstallScene(
            "Assets/Scenes/Stage3_Scene.unity",
            stage3
        );
        EnsureBuildScenes();

        AssetDatabase.SaveAssets();
        Debug.Log("[StageSystemSetup] Installation complete.");
    }

    private static Dictionary<string, GameObject> LoadMonsterPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets/Sprites/Monster" }
        );

        return guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(prefab => prefab != null)
            .ToDictionary(prefab => prefab.name, prefab => prefab);
    }

    private static StageDefinition CreateDefinitionIfMissing(
        string assetPath,
        string stageName,
        bool finalStage,
        string nextSceneName,
        List<StageWaveDefinition> waves
    )
    {
        StageDefinition definition =
            AssetDatabase.LoadAssetAtPath<StageDefinition>(assetPath);

        if (definition != null)
        {
            return definition;
        }

        definition = ScriptableObject.CreateInstance<StageDefinition>();
        definition.ConfigureForEditor(
            stageName,
            finalStage,
            nextSceneName,
            3f,
            waves
        );
        AssetDatabase.CreateAsset(definition, assetPath);
        return definition;
    }

    private static void InstallScene(
        string scenePath,
        StageDefinition definition
    )
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;

        if (!wasLoaded)
        {
            scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Additive
            );
        }

        StageDirector director = scene.GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<StageDirector>(true)
            )
            .FirstOrDefault();

        if (director == null)
        {
            GameObject systemObject = new("StageSystem");
            SceneManager.MoveGameObjectToScene(systemObject, scene);
            director = systemObject.AddComponent<StageDirector>();

            for (int index = 0; index < SpawnPositions.Length; index++)
            {
                GameObject point = new($"SpawnPoint{index + 1}");
                point.transform.SetParent(systemObject.transform);
                point.transform.localPosition = SpawnPositions[index];
                point.AddComponent<MonsterSpawnPoint>();
            }
        }

        director.Configure(definition);
        EditorUtility.SetDirty(director);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!wasLoaded)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void EnsureBuildScenes()
    {
        string[] requiredScenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Stage1_Scene.unity",
            "Assets/Scenes/Stage2_Scene.unity",
            "Assets/Scenes/Stage3_Scene.unity",
            "Assets/Scenes/Ending.unity"
        };
        Dictionary<string, EditorBuildSettingsScene> existing =
            EditorBuildSettings.scenes.ToDictionary(
                scene => scene.path,
                scene => scene
            );

        EditorBuildSettings.scenes = requiredScenes
            .Select(path => existing.TryGetValue(path, out var scene)
                ? scene
                : new EditorBuildSettingsScene(path, true)
            )
            .Concat(
                EditorBuildSettings.scenes.Where(
                    scene => !requiredScenes.Contains(scene.path)
                )
            )
            .ToArray();
    }

    private static List<StageWaveDefinition> CreateStage1Waves(
        IReadOnlyDictionary<string, GameObject> monsters
    )
    {
        GameObject rabbit = monsters["Rabbit"];
        GameObject bird = monsters["Bird"];
        GameObject pig = monsters["Pig"];

        return CreateStandardWaves(
            rabbit,
            bird,
            pig,
            monsters["Hunter"]
        );
    }

    private static List<StageWaveDefinition> CreateStage2Waves(
        IReadOnlyDictionary<string, GameObject> monsters
    )
    {
        GameObject moth = monsters["Moth"];
        GameObject fairy = monsters["FlowerFairy"];
        GameObject teaCup = monsters["TeaCup"];

        return CreateStandardWaves(
            moth,
            fairy,
            teaCup,
            monsters["Grandma"]
        );
    }

    private static List<StageWaveDefinition> CreateStage3Waves(
        IReadOnlyDictionary<string, GameObject> monsters
    )
    {
        GameObject redString = monsters["RedString"];
        GameObject blanket = monsters["Blanket"];
        GameObject sign = monsters["Sign"];

        return CreateStandardWaves(
            redString,
            blanket,
            sign,
            monsters["DeerKing"]
        );
    }

    private static List<StageWaveDefinition> CreateStandardWaves(
        GameObject first,
        GameObject second,
        GameObject third,
        GameObject boss
    )
    {
        return new List<StageWaveDefinition>
        {
            Wave("기본 몬스터", 0f, 1.5f, 12, first),
            Wave(
                "두 번째 몬스터 추가",
                30f,
                1.3f,
                18,
                first,
                second
            ),
            Wave(
                "세 번째 몬스터 추가",
                60f,
                1.1f,
                24,
                first,
                second,
                third
            ),
            Wave(
                "혼합 웨이브",
                90f,
                0.85f,
                30,
                first,
                second,
                third
            ),
            new StageWaveDefinition(
                "보스",
                120f,
                1f,
                1,
                1,
                true,
                new MonsterSpawnEntry(boss)
            )
        };
    }

    private static StageWaveDefinition Wave(
        string name,
        float startTime,
        float interval,
        int maxAlive,
        params GameObject[] prefabs
    )
    {
        MonsterSpawnEntry[] entries = prefabs
            .Select(prefab => new MonsterSpawnEntry(prefab))
            .ToArray();

        return new StageWaveDefinition(
            name,
            startTime,
            interval,
            maxAlive,
            1,
            false,
            entries
        );
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int index = 1; index < parts.Length; index++)
        {
            string nextPath = currentPath + "/" + parts[index];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[index]);
            }

            currentPath = nextPath;
        }
    }
}
