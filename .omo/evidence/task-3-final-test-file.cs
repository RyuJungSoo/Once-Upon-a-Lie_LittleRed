using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ExperienceProgressSceneTests
{
    private const string Stage1ScenePath =
        "Assets/Scenes/Stage1_Scene.unity";

    private const string SourcePrefabPath =
        "Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab";

    private const string FillerPath =
        "Canvas/Exp_UI/Bar_1/Filler";

    private const string LevelTextPath =
        "Canvas/Exp_UI/Text (TMP)";

    private const long FillerSourceFileId =
        114361196886809056L;

    private static readonly Color ExpectedFillerColor =
        new Color(0.5568628f, 0.7725490f, 0.2901961f, 1f);

    [Test]
    public void Stage1ExperienceGaugeUsesOpaqueGreenFilledImage()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        string activeScenePath = SceneManager.GetActiveScene().path;

        try
        {
            EditorSceneManager.OpenScene(
                Stage1ScenePath,
                OpenSceneMode.Single
            );

            Image fillerImage =
                FindRequiredComponent<Image>(FillerPath);
            Component levelText =
                FindRequiredComponent(
                    LevelTextPath,
                    "TMPro.TMP_Text, Unity.TextMeshPro"
                );

            Assert.That(
                GetSourceFileId(fillerImage),
                Is.EqualTo(FillerSourceFileId)
            );
            Assert.That(
                GetScenePath(fillerImage.transform),
                Is.EqualTo(FillerPath)
            );
            Assert.That(
                GetScenePath(levelText.transform),
                Is.EqualTo(LevelTextPath)
            );

            AssertSceneImage(fillerImage);
            AssertUIManagerReferences(fillerImage, levelText);
            AssertSourcePrefabUnchanged();
        }
        finally
        {
            RestoreActiveScene(previousSetup, activeScenePath);
        }
    }

    private static void AssertSceneImage(Image image)
    {
        Assert.That(
            image.color.r,
            Is.EqualTo(ExpectedFillerColor.r).Within(0.001f)
        );
        Assert.That(
            image.color.g,
            Is.EqualTo(ExpectedFillerColor.g).Within(0.001f)
        );
        Assert.That(
            image.color.b,
            Is.EqualTo(ExpectedFillerColor.b).Within(0.001f)
        );
        Assert.That(image.color.a, Is.EqualTo(1f));
        Assert.That(image.type, Is.EqualTo(Image.Type.Filled));
        Assert.That(
            image.fillMethod,
            Is.EqualTo(Image.FillMethod.Horizontal)
        );
        Assert.That(image.fillOrigin, Is.EqualTo(0));
    }

    private static void AssertUIManagerReferences(
        Image expectedGauge,
        Component expectedLevelText
    )
    {
        GameObject managerObject = GameObject.Find("UIManager");
        Assert.That(managerObject, Is.Not.Null);

        System.Type managerType =
            System.Type.GetType("UIManager, Assembly-CSharp");
        Assert.That(managerType, Is.Not.Null);

        Component manager = managerObject.GetComponent(
            managerType
        );

        Assert.That(manager, Is.Not.Null);

        SerializedObject serializedManager =
            new SerializedObject(manager);
        Assert.That(
            serializedManager
                .FindProperty("experienceGauge")
                .objectReferenceValue,
            Is.SameAs(expectedGauge)
        );
        Assert.That(
            serializedManager
                .FindProperty("levelText")
                .objectReferenceValue,
            Is.SameAs(expectedLevelText)
        );
    }

    private static void AssertSourcePrefabUnchanged()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SourcePrefabPath
            );
        Assert.That(prefab, Is.Not.Null);

        Image sourceFiller =
            prefab.transform
                .Find("Filler")
                .GetComponent<Image>();

        Assert.That(sourceFiller, Is.Not.Null);
        Assert.That(
            GetSourceFileId(sourceFiller),
            Is.EqualTo(FillerSourceFileId)
        );
        Assert.That(
            sourceFiller.color.r,
            Is.EqualTo(1f).Within(0.001f)
        );
        Assert.That(
            sourceFiller.color.g,
            Is.EqualTo(1f).Within(0.001f)
        );
        Assert.That(
            sourceFiller.color.b,
            Is.EqualTo(1f).Within(0.001f)
        );
        Assert.That(
            sourceFiller.color.a,
            Is.EqualTo(0.48235294f).Within(0.001f)
        );
        Assert.That(
            sourceFiller.type,
            Is.EqualTo(Image.Type.Filled)
        );
        Assert.That(
            sourceFiller.fillMethod,
            Is.EqualTo(Image.FillMethod.Horizontal)
        );
        Assert.That(sourceFiller.fillOrigin, Is.EqualTo(0));
    }

    private static T FindRequiredComponent<T>(
        string path
    ) where T : Component
    {
        GameObject gameObject = GameObject.Find(path);
        Assert.That(gameObject, Is.Not.Null, path);

        T component = gameObject.GetComponent<T>();
        Assert.That(component, Is.Not.Null, path);
        return component;
    }

    private static Component FindRequiredComponent(
        string path,
        string componentTypeName
    )
    {
        GameObject gameObject = GameObject.Find(path);
        Assert.That(gameObject, Is.Not.Null, path);

        System.Type componentType =
            System.Type.GetType(componentTypeName);
        Assert.That(componentType, Is.Not.Null, componentTypeName);

        Component component =
            gameObject.GetComponent(componentType);
        Assert.That(component, Is.Not.Null, path);
        return component;
    }

    private static string GetScenePath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static long GetSourceFileId(Object target)
    {
        Object source =
            PrefabUtility.GetCorrespondingObjectFromSource(target);

        GlobalObjectId globalObjectId =
            GlobalObjectId.GetGlobalObjectIdSlow(
                source == null ? target : source
            );

        return (long)globalObjectId.targetObjectId;
    }

    private static void RestoreActiveScene(
        SceneSetup[] previousSetup,
        string activeScenePath
    )
    {
        if (previousSetup.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(
                previousSetup
            );
        }

        if (string.IsNullOrEmpty(activeScenePath))
        {
            return;
        }

        Scene activeScene =
            SceneManager.GetSceneByPath(activeScenePath);
        if (!activeScene.IsValid())
        {
            activeScene = EditorSceneManager.OpenScene(
                activeScenePath,
                OpenSceneMode.Single
            );
        }

        if (activeScene.IsValid())
        {
            SceneManager.SetActiveScene(activeScene);
        }
    }
}
