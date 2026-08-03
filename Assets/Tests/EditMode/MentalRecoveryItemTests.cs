using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MentalRecoveryItemTests
{
    private static readonly Type PlayerMentalType =
        Type.GetType("PlayerMental, Assembly-CSharp");

    private static readonly Type PlayerLevelStatsType =
        Type.GetType("PlayerLevelStats, Assembly-CSharp");

    private static readonly Type MentalRecoveryItemType =
        Type.GetType("MentalRecoveryItem, Assembly-CSharp");

    private GameObject mentalObject;
    private GameObject itemObject;
    private Component playerMental;
    private Component item;

    [SetUp]
    public void SetUp()
    {
        Assert.That(PlayerMentalType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);
        Assert.That(MentalRecoveryItemType, Is.Not.Null);

        mentalObject = new GameObject("Test Player Mental");
        mentalObject.SetActive(false);
        playerMental = mentalObject.AddComponent(
            PlayerMentalType
        );

        Component levelStats = mentalObject.GetComponent(
            PlayerLevelStatsType
        );
        Invoke(levelStats, "RecalculateStats", 1);

        mentalObject.SetActive(true);
        Invoke(playerMental, "ResetMental");

        itemObject = new GameObject("Test Mental Item");
        itemObject.SetActive(false);
        itemObject.AddComponent<BoxCollider2D>();
        item = itemObject.AddComponent(
            MentalRecoveryItemType
        );
        itemObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(itemObject);
        UnityEngine.Object.DestroyImmediate(mentalObject);

        ItemPoolTestCleanup.DestroyPoolObjects();
    }

    [TestCase(0.08f, 8f)]
    [TestCase(0.15f, 15f)]
    [TestCase(0.30f, 30f)]
    public void ItemRestoresConfiguredMaxMentalRatio(
        float restoreRatio,
        float expectedMental
    )
    {
        Invoke(playerMental, "SetMental", 0f);
        ConfigureItem(restoreRatio, 0f, 0f);

        bool applied = (bool)Invoke(
            item,
            "TryApply",
            playerMental
        );

        Assert.That(applied, Is.True);
        Assert.That(
            (float)GetProperty(
                playerMental,
                "CurrentMental"
            ),
            Is.EqualTo(expectedMental).Within(0.001f)
        );
    }

    [Test]
    public void PieProtectionBlocksIncomingDamageButNotRawDrain()
    {
        Invoke(playerMental, "SetMental", 50f);
        ConfigureItem(0.20f, 3f, 3f);
        Invoke(item, "TryApply", playerMental);

        Invoke(playerMental, "TakeMentalDamage", 10f);

        Assert.That(
            (float)GetProperty(
                playerMental,
                "CurrentMental"
            ),
            Is.EqualTo(70f).Within(0.001f)
        );
        Assert.That(
            (bool)GetProperty(
                playerMental,
                "IsPassiveMentalDrainPaused"
            ),
            Is.True
        );
        Assert.That(
            (bool)GetProperty(
                playerMental,
                "IsIncomingMentalDamageBlocked"
            ),
            Is.True
        );

        Invoke(playerMental, "DecreaseMentalRaw", 10f);

        Assert.That(
            (float)GetProperty(
                playerMental,
                "CurrentMental"
            ),
            Is.EqualTo(60f).Within(0.001f)
        );
    }

    [TestCase("RedBerry_low", 0.15f, 0f, 0f, 5)]
    [TestCase("RedBerry_medium", 0.20f, 0f, 0f, 5)]
    [TestCase("RedBerry_high", 0.25f, 0f, 0f, 5)]
    [TestCase("StarCandy_low", 0.08f, 5f, 0f, 6)]
    [TestCase("StarCandy_medium", 0.10f, 6f, 0f, 6)]
    [TestCase("StarCandy_high", 0.12f, 7f, 0f, 6)]
    [TestCase("Pie_low", 0.20f, 3f, 3f, 7)]
    [TestCase("Pie_medium", 0.25f, 4f, 4f, 7)]
    [TestCase("Pie_high", 0.30f, 5f, 5f, 7)]
    public void ItemPrefabHasConfiguredMentalEffect(
        string prefabName,
        float restoreRatio,
        float passivePause,
        float damageBlock,
        int pickupSfxType
    )
    {
        string path =
            $"Assets/Sprites/Item/{prefabName}.prefab";
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                path
            );

        Assert.That(prefab, Is.Not.Null);

        Component effect = prefab.GetComponent(
            MentalRecoveryItemType
        );
        BoxCollider2D itemCollider =
            prefab.GetComponent<BoxCollider2D>();

        Assert.That(effect, Is.Not.Null);
        Assert.That(itemCollider, Is.Not.Null);
        Assert.That(itemCollider.isTrigger, Is.True);

        Assert.That(
            (float)GetProperty(
                effect,
                "MentalRestoreRatio"
            ),
            Is.EqualTo(restoreRatio).Within(0.001f)
        );
        Assert.That(
            (float)GetProperty(
                effect,
                "PassiveMentalDrainPauseDuration"
            ),
            Is.EqualTo(passivePause).Within(0.001f)
        );
        Assert.That(
            (float)GetProperty(
                effect,
                "IncomingMentalDamageBlockDuration"
            ),
            Is.EqualTo(damageBlock).Within(0.001f)
        );
        Assert.That(
            Convert.ToInt32(
                GetProperty(
                    effect,
                    "PickupSfxType"
                )
            ),
            Is.EqualTo(pickupSfxType)
        );
    }

    private void ConfigureItem(
        float restoreRatio,
        float passivePause,
        float damageBlock
    )
    {
        SerializedObject serializedItem =
            new SerializedObject(item);

        serializedItem
            .FindProperty("mentalRestoreRatio")
            .floatValue = restoreRatio;
        serializedItem
            .FindProperty(
                "passiveMentalDrainPauseDuration"
            )
            .floatValue = passivePause;
        serializedItem
            .FindProperty(
                "incomingMentalDamageBlockDuration"
            )
            .floatValue = damageBlock;

        serializedItem.ApplyModifiedPropertiesWithoutUndo();
    }

    private static object GetProperty(
        object target,
        string propertyName
    )
    {
        PropertyInfo property = target
            .GetType()
            .GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public
            );

        Assert.That(property, Is.Not.Null);
        return property.GetValue(target);
    }

    private static object Invoke(
        object target,
        string methodName,
        params object[] arguments
    )
    {
        MethodInfo method = target
            .GetType()
            .GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

        Assert.That(method, Is.Not.Null);
        return method.Invoke(target, arguments);
    }

}
