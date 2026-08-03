using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ItemRenderOrderPlayModeTests
{
    [UnityTest]
    public IEnumerator Stage1ItemsReceiveStableOrder()
    {
        yield return VerifyScene(
            "Assets/Scenes/Stage1_Scene.unity",
            4
        );
    }

    [UnityTest]
    public IEnumerator Stage2ItemsReceiveStableOrder()
    {
        yield return VerifyScene(
            "Assets/Scenes/Stage2_Scene.unity",
            4
        );
    }

    [UnityTest]
    public IEnumerator Stage3ItemsReceiveStableOrder()
    {
        yield return VerifyScene(
            "Assets/Scenes/Stage3_Scene.unity",
            4
        );
    }

    private static IEnumerator VerifyScene(
        string scenePath,
        int expectedItemCount
    )
    {
        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                scenePath,
                LoadSceneMode.Single
            );

        yield return loadOperation;

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] itemRoots = scene
            .GetRootGameObjects()
            .SelectMany(root =>
                root.GetComponentsInChildren<Transform>(true)
            )
            .Where(transform =>
                transform.gameObject.layer ==
                LayerMask.NameToLayer("Item")
            )
            .Where(transform =>
                transform.parent == null ||
                transform.parent.gameObject.layer !=
                LayerMask.NameToLayer("Item")
            )
            .Select(transform => transform.gameObject)
            .ToArray();

        Assert.That(
            itemRoots,
            Has.Length.EqualTo(expectedItemCount)
        );

        SortingGroup[] sortingGroups = itemRoots
            .Select(item => item.GetComponent<SortingGroup>())
            .ToArray();

        Assert.That(
            sortingGroups.All(group => group != null),
            Is.True
        );
        Assert.That(
            sortingGroups
                .Select(group => group.sortingOrder)
                .Distinct()
                .Count(),
            Is.EqualTo(expectedItemCount)
        );
    }
}
