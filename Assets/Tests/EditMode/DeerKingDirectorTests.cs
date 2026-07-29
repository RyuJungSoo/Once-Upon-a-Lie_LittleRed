using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DeerKingDirectorTests
{
    private const string DeerKingFolder =
        "Assets/Sprites/Monster/DeerKing";

    private static readonly Type DirectorType =
        Type.GetType("DeerKingDirector, Assembly-CSharp");
    private static readonly Type ProfileType =
        Type.GetType(
            "DeerKingBossProfile, Assembly-CSharp"
        );
    private static readonly Type AimedChargeType =
        Type.GetType(
            "MonsterAimedChargeAttack, Assembly-CSharp"
        );
    private static readonly Type RangedAttackType =
        Type.GetType(
            "MonsterRangedAttack, Assembly-CSharp"
        );
    private static readonly Type MonsterAttackType =
        Type.GetType("MonsterAttack, Assembly-CSharp");
    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");
    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");
    private static readonly Type MonsterProjectileType =
        Type.GetType(
            "MonsterProjectile, Assembly-CSharp"
        );

    [SetUp]
    public void SetUp()
    {
        Assert.That(DirectorType, Is.Not.Null);
        Assert.That(ProfileType, Is.Not.Null);
        Assert.That(AimedChargeType, Is.Not.Null);
        Assert.That(RangedAttackType, Is.Not.Null);
        Assert.That(MonsterAttackType, Is.Not.Null);
        Assert.That(MonsterChaseType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);
        Assert.That(MonsterProjectileType, Is.Not.Null);
    }

    [Test]
    public void DeerKingPrefabOwnsThreeConfiguredPatterns()
    {
        GameObject prefab = LoadDeerKingPrefab();
        UnityEngine.Object profile =
            AssetDatabase.LoadAssetAtPath(
                $"{DeerKingFolder}/DeerKingBossProfile.asset",
                ProfileType
            );
        GameObject bullet =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Mob_Bullet1.prefab"
            );

        Component director =
            prefab.GetComponent(DirectorType);
        Component aimedCharge =
            prefab.GetComponent(AimedChargeType);
        Component health =
            prefab.GetComponent(MonsterHealthType);
        object stats = GetProperty(health, "Stats");
        object rangedSettings =
            GetProperty(stats, "RangedAttack");

        Assert.That(director, Is.Not.Null);
        Assert.That(profile, Is.Not.Null);
        Assert.That(
            prefab.GetComponent(MonsterAttackType),
            Is.Not.Null
        );
        Assert.That(
            prefab.GetComponent(MonsterChaseType),
            Is.Not.Null
        );
        Assert.That(
            prefab.GetComponent(RangedAttackType),
            Is.Not.Null
        );
        Assert.That(aimedCharge, Is.Not.Null);
        Assert.That(
            GetProperty(director, "Profile"),
            Is.SameAs(profile)
        );
        Assert.That(
            GetProperty(aimedCharge, "AutomaticActivation"),
            Is.False
        );
        Assert.That(
            GetProperty(rangedSettings, "ProjectilePrefab"),
            Is.SameAs(
                bullet.GetComponent(MonsterProjectileType)
            )
        );
    }

    [Test]
    public void BossProfileExposesValidatedPatternSettings()
    {
        UnityEngine.Object profile =
            AssetDatabase.LoadAssetAtPath(
                $"{DeerKingFolder}/DeerKingBossProfile.asset",
                ProfileType
            );

        Assert.That(profile, Is.Not.Null);
        Assert.That(
            GetProperty<float>(profile, "RamDuration"),
            Is.EqualTo(2.5f)
        );
        Assert.That(
            GetProperty<float>(profile, "RangedDuration"),
            Is.EqualTo(3f)
        );
        Assert.That(
            GetProperty<float>(
                profile,
                "RecoveryDuration"
            ),
            Is.EqualTo(0.75f)
        );

        object charge =
            GetProperty(profile, "AimedCharge");

        Assert.That(
            GetProperty<float>(charge, "ChargeRange"),
            Is.EqualTo(4f)
        );
        Assert.That(
            GetProperty<float>(charge, "AimDuration"),
            Is.EqualTo(0.65f)
        );
        Assert.That(
            GetProperty<float>(charge, "ChargeSpeed"),
            Is.EqualTo(8f)
        );
        Assert.That(
            GetProperty<float>(
                charge,
                "ChargeDuration"
            ),
            Is.EqualTo(0.75f)
        );
        Assert.That(
            GetProperty<float>(
                charge,
                "RecoveryDuration"
            ),
            Is.EqualTo(0.65f)
        );
        Assert.That(
            GetProperty<float>(
                charge,
                "ChargeCooldown"
            ),
            Is.EqualTo(1.5f)
        );
    }

    [Test]
    public void DirectorMakesPatternsMutuallyExclusive()
    {
        GameObject deerKing =
            UnityEngine.Object.Instantiate(
                LoadDeerKingPrefab()
            );
        GameObject target =
            new GameObject("DeerKing Test Target");

        try
        {
            Component director =
                deerKing.GetComponent(DirectorType);
            Behaviour chase =
                (Behaviour)deerKing.GetComponent(
                    MonsterChaseType
                );
            Behaviour contact =
                (Behaviour)deerKing.GetComponent(
                    MonsterAttackType
                );
            Behaviour ranged =
                (Behaviour)deerKing.GetComponent(
                    RangedAttackType
                );
            Component aimedCharge =
                deerKing.GetComponent(AimedChargeType);

            Invoke(aimedCharge, "Awake");
            Invoke(ranged, "Awake");
            Invoke(director, "Awake");

            SetField(
                chase,
                "target",
                target.transform
            );

            Invoke(director, "EnterRam");
            AssertPattern(director, "Ram");
            Assert.That(chase.enabled, Is.True);
            Assert.That(contact.enabled, Is.True);
            Assert.That(ranged.enabled, Is.False);
            Assert.That(
                GetProperty(
                    aimedCharge,
                    "IsRunning"
                ),
                Is.False
            );

            Invoke(director, "EnterRanged");
            AssertPattern(director, "Ranged");
            Assert.That(chase.enabled, Is.True);
            Assert.That(contact.enabled, Is.False);
            Assert.That(ranged.enabled, Is.True);
            Assert.That(
                GetProperty(
                    aimedCharge,
                    "IsRunning"
                ),
                Is.False
            );

            Invoke(director, "EnterAimedCharge");
            AssertPattern(director, "AimedCharge");
            Assert.That(chase.enabled, Is.False);
            Assert.That(contact.enabled, Is.False);
            Assert.That(ranged.enabled, Is.False);
            Assert.That(
                GetProperty(
                    aimedCharge,
                    "IsRunning"
                ),
                Is.True
            );

            Invoke(director, "EnterRecovery");
            AssertPattern(director, "Recovery");
            Assert.That(chase.enabled, Is.False);
            Assert.That(contact.enabled, Is.False);
            Assert.That(ranged.enabled, Is.False);
            Assert.That(
                GetProperty(
                    aimedCharge,
                    "IsRunning"
                ),
                Is.False
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(deerKing);
        }
    }

    private static GameObject LoadDeerKingPrefab()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{DeerKingFolder}/DeerKing.prefab"
            );

        Assert.That(prefab, Is.Not.Null);
        return prefab;
    }

    private static void AssertPattern(
        object director,
        string expected
    )
    {
        Assert.That(
            GetProperty(director, "CurrentPattern")
                .ToString(),
            Is.EqualTo(expected)
        );
    }

    private static void SetField(
        object target,
        string fieldName,
        object value
    )
    {
        FieldInfo field = target
            .GetType()
            .GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.NonPublic
            );

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
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

    private static T GetProperty<T>(
        object target,
        string propertyName
    )
    {
        return (T)GetProperty(target, propertyName);
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
