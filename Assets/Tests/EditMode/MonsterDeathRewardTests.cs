using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static MonsterDeathRewardTestSupport;

public sealed class MonsterDeathRewardTests
{
    [TearDown]
    public void TearDown()
    {
        ItemPoolTestCleanup.DestroyPoolObjects();
    }

    [TestCase("Unspecified", 0)]
    [TestCase("SelfDestruct", 0)]
    [TestCase("PlayerBullet", 3)]
    [TestCase("Invalid", 0)]
    public void LethalDamageRewardsDependOnSource(
        string damageSourceName,
        int expectedRecoveryItemCount
    )
    {
        HashSet<int> existingDropIds =
            CaptureRuntimeDropIds();
        ScriptableObject stats = null;
        GameObject monster = null;

        try
        {
            Component health = CreateConfiguredMonster(
                out stats,
                out monster
            );
            object damageSource = CreateDamageSource(
                damageSourceName
            );

            ExpectEditModeDestroyError();
            Invoke(
                health,
                "TakeDamage",
                1,
                damageSource
            );

            AssertNewDropCounts(
                existingDropIds,
                expectedRecoveryItemCount
            );
        }
        finally
        {
            DestroyNewDrops(existingDropIds);

            if (monster != null)
            {
                UnityEngine.Object.DestroyImmediate(monster);
            }

            if (stats != null)
            {
                UnityEngine.Object.DestroyImmediate(stats);
            }
        }
    }

    [Test]
    public void LethalHealthChangedReentryCannotDuplicateRewards()
    {
        HashSet<int> existingDropIds =
            CaptureRuntimeDropIds();
        ScriptableObject stats = null;
        GameObject monster = null;

        try
        {
            Component health = CreateConfiguredMonster(
                out stats,
                out monster
            );
            object playerBullet = CreateDamageSource(
                "PlayerBullet"
            );
            object selfDestruct = CreateDamageSource(
                "SelfDestruct"
            );
            EventInfo healthChanged =
                MonsterHealthType.GetEvent("HealthChanged");

            Assert.That(healthChanged, Is.Not.Null);
            healthChanged.AddEventHandler(
                health,
                new Action<int, int>((_, _) =>
                    Invoke(
                        health,
                        "TakeDamage",
                        1,
                        playerBullet
                    ))
            );

            ExpectEditModeDestroyError();
            Invoke(
                health,
                "TakeDamage",
                1,
                selfDestruct
            );

            AssertNewDropCounts(existingDropIds, 0);
        }
        finally
        {
            DestroyNewDrops(existingDropIds);

            if (monster != null)
            {
                UnityEngine.Object.DestroyImmediate(monster);
            }

            if (stats != null)
            {
                UnityEngine.Object.DestroyImmediate(stats);
            }
        }
    }

    [Test]
    public void SelfDestructComponentIsBlanketExclusive()
    {
        Assert.That(
            BlanketContactRestraintType,
            Is.Not.Null
        );

        string[] prefabPaths = AssetDatabase
            .FindAssets("t:Prefab", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(ContainsSelfDestructComponent)
            .ToArray();

        Assert.That(
            prefabPaths,
            Is.EqualTo(new[] { BlanketPrefabPath })
        );
    }
}
