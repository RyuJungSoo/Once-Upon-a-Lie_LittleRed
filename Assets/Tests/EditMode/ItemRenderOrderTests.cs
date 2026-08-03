using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ItemRenderOrderTests
{
    private static readonly Type ItemRenderOrderType =
        Type.GetType("ItemRenderOrder, Assembly-CSharp");

    [TestCase(
        "Assets/Sprites/Item/ExpCrystal_low.prefab",
        "Assets/Sprites/Item/RedBerry_low.prefab"
    )]
    [TestCase(
        "Assets/Sprites/Item/ExpCrystal_medium.prefab",
        "Assets/Sprites/Item/Pie_medium.prefab"
    )]
    [TestCase(
        "Assets/Sprites/Item/ExpCrystal_medium.prefab",
        "Assets/Sprites/Item/StarCandy_medium.prefab"
    )]
    [TestCase(
        "Assets/Sprites/Item/ExpCrystal_high.prefab",
        "Assets/Sprites/Item/StarCandy_high.prefab"
    )]
    public void ReportedPairsKeepOrderAcrossCrystalFrames(
        string crystalPath,
        string itemPath
    )
    {
        GameObject crystal = InstantiatePrefab(crystalPath);
        GameObject item = InstantiatePrefab(itemPath);

        try
        {
            AssignRenderOrder(crystal);
            AssignRenderOrder(item);

            SortingGroup crystalGroup =
                crystal.GetComponent<SortingGroup>();
            SortingGroup itemGroup =
                item.GetComponent<SortingGroup>();

            Assert.That(crystalGroup, Is.Not.Null);
            Assert.That(itemGroup, Is.Not.Null);
            Assert.That(
                crystalGroup.sortingOrder,
                Is.Not.EqualTo(itemGroup.sortingOrder)
            );

            AssertOrderAcrossAnimationFrames(
                crystal,
                crystalGroup.sortingOrder
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(crystal);
            UnityEngine.Object.DestroyImmediate(item);
        }
    }

    private static GameObject InstantiatePrefab(string path)
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null, path);

        return UnityEngine.Object.Instantiate(
            prefab,
            Vector3.zero,
            Quaternion.identity
        );
    }

    private static void AssignRenderOrder(GameObject item)
    {
        Assert.That(ItemRenderOrderType, Is.Not.Null);
        MethodInfo assignMethod =
            ItemRenderOrderType.GetMethod(
                "Assign",
                BindingFlags.Static |
                BindingFlags.Public
            );
        Assert.That(assignMethod, Is.Not.Null);
        assignMethod.Invoke(null, new object[] { item });
    }

    private static void AssertOrderAcrossAnimationFrames(
        GameObject crystal,
        int expectedOrder
    )
    {
        Animator animator = crystal.GetComponent<Animator>();
        Assert.That(animator, Is.Not.Null);
        AnimationClip[] clips =
            animator.runtimeAnimatorController.animationClips;
        Assert.That(clips, Has.Length.EqualTo(1));

        AnimationMode.StartAnimationMode();

        try
        {
            float[] frameTimes = { 0f, 0.125f, 0.25f, 0.375f };

            foreach (float frameTime in frameTimes)
            {
                AnimationMode.SampleAnimationClip(
                    crystal,
                    clips[0],
                    frameTime
                );
                Assert.That(
                    crystal
                        .GetComponent<SortingGroup>()
                        .sortingOrder,
                    Is.EqualTo(expectedOrder)
                );
            }
        }
        finally
        {
            AnimationMode.StopAnimationMode();
        }
    }
}
