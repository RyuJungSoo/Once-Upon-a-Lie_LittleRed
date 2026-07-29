using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HunterDirectorTests
{
    private const string HunterFolder =
        "Assets/Sprites/Monster/Hunter";

    private static readonly Type DirectorType =
        Type.GetType("HunterDirector, Assembly-CSharp");
    private static readonly Type ProfileType =
        Type.GetType("HunterBossProfile, Assembly-CSharp");
    private static readonly Type MonsterAttackType =
        Type.GetType("MonsterAttack, Assembly-CSharp");
    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");
    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");
    private static readonly Type MonsterProjectileType =
        Type.GetType("MonsterProjectile, Assembly-CSharp");
    private static readonly Type MothFanAttackType =
        Type.GetType("MothFanAttack, Assembly-CSharp");
    private static readonly Type SignZigzagAttackType =
        Type.GetType("SignZigzagAttack, Assembly-CSharp");

    [SetUp]
    public void SetUp()
    {
        Assert.That(DirectorType, Is.Not.Null);
        Assert.That(ProfileType, Is.Not.Null);
        Assert.That(MonsterAttackType, Is.Not.Null);
        Assert.That(MonsterChaseType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);
        Assert.That(MonsterProjectileType, Is.Not.Null);
        Assert.That(MothFanAttackType, Is.Not.Null);
        Assert.That(SignZigzagAttackType, Is.Not.Null);
    }

    [Test]
    public void HunterPrefabOwnsBirdMothAndSignPatterns()
    {
        GameObject prefab = LoadHunterPrefab();
        UnityEngine.Object profile =
            AssetDatabase.LoadAssetAtPath(
                $"{HunterFolder}/HunterBossProfile.asset",
                ProfileType
            );
        GameObject bullet =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Mob_Bullet1.prefab"
            );
        Component director =
            prefab.GetComponent(DirectorType);
        Component health =
            prefab.GetComponent(MonsterHealthType);
        object settings = GetProperty(
            GetProperty(health, "Stats"),
            "RangedAttack"
        );

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
            prefab.GetComponent(MothFanAttackType),
            Is.Not.Null
        );
        Assert.That(
            prefab.GetComponent(SignZigzagAttackType),
            Is.Not.Null
        );
        Assert.That(
            GetProperty(director, "Profile"),
            Is.SameAs(profile)
        );
        Assert.That(
            GetProperty(settings, "ProjectilePrefab"),
            Is.SameAs(
                bullet.GetComponent(MonsterProjectileType)
            )
        );
    }

    [Test]
    public void BossProfileExposesPatternDurations()
    {
        UnityEngine.Object profile =
            AssetDatabase.LoadAssetAtPath(
                $"{HunterFolder}/HunterBossProfile.asset",
                ProfileType
            );

        Assert.That(profile, Is.Not.Null);
        Assert.That(
            GetProperty<float>(profile, "BirdDuration"),
            Is.EqualTo(2.5f)
        );
        Assert.That(
            GetProperty<float>(profile, "MothDuration"),
            Is.EqualTo(3f)
        );
        Assert.That(
            GetProperty<float>(profile, "SignDuration"),
            Is.EqualTo(2.5f)
        );
        Assert.That(
            GetProperty<float>(profile, "RecoveryDuration"),
            Is.EqualTo(0.75f)
        );
    }

    [Test]
    public void DirectorMakesPatternsMutuallyExclusive()
    {
        GameObject hunter =
            UnityEngine.Object.Instantiate(
                LoadHunterPrefab()
            );

        try
        {
            Component director =
                hunter.GetComponent(DirectorType);
            Behaviour chase =
                (Behaviour)hunter.GetComponent(
                    MonsterChaseType
                );
            Behaviour contact =
                (Behaviour)hunter.GetComponent(
                    MonsterAttackType
                );
            Behaviour moth =
                (Behaviour)hunter.GetComponent(
                    MothFanAttackType
                );
            Behaviour sign =
                (Behaviour)hunter.GetComponent(
                    SignZigzagAttackType
                );

            Invoke(moth, "Awake");
            Invoke(sign, "Awake");
            Invoke(director, "Awake");

            Invoke(director, "EnterBird");
            AssertPattern(director, "Bird");
            Assert.That(chase.enabled, Is.True);
            Assert.That(contact.enabled, Is.True);
            Assert.That(moth.enabled, Is.False);
            Assert.That(sign.enabled, Is.False);

            Invoke(director, "EnterMoth");
            AssertPattern(director, "Moth");
            Assert.That(chase.enabled, Is.True);
            Assert.That(contact.enabled, Is.False);
            Assert.That(moth.enabled, Is.True);
            Assert.That(sign.enabled, Is.False);

            Invoke(director, "EnterSign");
            AssertPattern(director, "Sign");
            Assert.That(chase.enabled, Is.False);
            Assert.That(contact.enabled, Is.False);
            Assert.That(moth.enabled, Is.False);
            Assert.That(sign.enabled, Is.True);

            Invoke(director, "EnterRecovery");
            AssertPattern(director, "Recovery");
            Assert.That(chase.enabled, Is.False);
            Assert.That(contact.enabled, Is.False);
            Assert.That(moth.enabled, Is.False);
            Assert.That(sign.enabled, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hunter);
        }
    }

    private static GameObject LoadHunterPrefab()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{HunterFolder}/Hunter.prefab"
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
        string methodName
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
        return method.Invoke(target, null);
    }
}
