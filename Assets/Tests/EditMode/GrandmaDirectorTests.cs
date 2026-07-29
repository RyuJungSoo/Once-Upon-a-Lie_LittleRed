using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GrandmaDirectorTests
{
    private const string GrandmaFolder =
        "Assets/Sprites/Monster/Grandma";

    private static readonly Type DirectorType =
        Type.GetType("GrandmaDirector, Assembly-CSharp");
    private static readonly Type ProfileType =
        Type.GetType(
            "GrandmaBossProfile, Assembly-CSharp"
        );
    private static readonly Type RestraintAttackType =
        Type.GetType(
            "GrandmaRestraintAttack, Assembly-CSharp"
        );
    private static readonly Type TeaCupAttackType =
        Type.GetType(
            "TeaCupBarrageAttack, Assembly-CSharp"
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
        Assert.That(RestraintAttackType, Is.Not.Null);
        Assert.That(TeaCupAttackType, Is.Not.Null);
        Assert.That(MonsterAttackType, Is.Not.Null);
        Assert.That(MonsterChaseType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);
        Assert.That(MonsterProjectileType, Is.Not.Null);
    }

    [Test]
    public void GrandmaPrefabOwnsTeaCupBlanketAndRedStringPatterns()
    {
        GameObject prefab = LoadGrandmaPrefab();
        UnityEngine.Object profile =
            AssetDatabase.LoadAssetAtPath(
                $"{GrandmaFolder}/GrandmaBossProfile.asset",
                ProfileType
            );
        GameObject bullet =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Mob_Bullet1.prefab"
            );
        Component director =
            prefab.GetComponent(DirectorType);
        Component restraint =
            prefab.GetComponent(RestraintAttackType);
        Component health =
            prefab.GetComponent(MonsterHealthType);
        object settings = GetProperty(
            GetProperty(health, "Stats"),
            "RangedAttack"
        );

        Assert.That(director, Is.Not.Null);
        Assert.That(restraint, Is.Not.Null);
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
            prefab.GetComponent(TeaCupAttackType),
            Is.Not.Null
        );
        Assert.That(
            GetProperty(director, "Profile"),
            Is.SameAs(profile)
        );
        Assert.That(
            GetProperty(restraint, "Profile"),
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
    public void BossProfileExposesPatternAndRestraintDurations()
    {
        UnityEngine.Object profile =
            AssetDatabase.LoadAssetAtPath(
                $"{GrandmaFolder}/GrandmaBossProfile.asset",
                ProfileType
            );

        Assert.That(profile, Is.Not.Null);
        Assert.That(
            GetProperty<float>(profile, "TeaCupDuration"),
            Is.EqualTo(3f)
        );
        Assert.That(
            GetProperty<float>(profile, "BlanketDuration"),
            Is.EqualTo(2.5f)
        );
        Assert.That(
            GetProperty<float>(profile, "RedStringDuration"),
            Is.EqualTo(2.5f)
        );
        Assert.That(
            GetProperty<float>(profile, "RecoveryDuration"),
            Is.EqualTo(0.75f)
        );
        Assert.That(
            GetProperty<float>(profile, "RestraintDuration"),
            Is.EqualTo(1.5f)
        );
    }

    [Test]
    public void DirectorMakesPatternsMutuallyExclusive()
    {
        GameObject grandma =
            UnityEngine.Object.Instantiate(
                LoadGrandmaPrefab()
            );

        try
        {
            Component director =
                grandma.GetComponent(DirectorType);
            Behaviour chase =
                (Behaviour)grandma.GetComponent(
                    MonsterChaseType
                );
            Behaviour contact =
                (Behaviour)grandma.GetComponent(
                    MonsterAttackType
                );
            Behaviour teaCup =
                (Behaviour)grandma.GetComponent(
                    TeaCupAttackType
                );
            Behaviour restraint =
                (Behaviour)grandma.GetComponent(
                    RestraintAttackType
                );

            Invoke(teaCup, "Awake");
            Invoke(restraint, "Awake");
            Invoke(director, "Awake");

            Invoke(director, "EnterTeaCup");
            AssertPattern(director, "TeaCup");
            Assert.That(chase.enabled, Is.False);
            Assert.That(contact.enabled, Is.False);
            Assert.That(teaCup.enabled, Is.True);
            Assert.That(restraint.enabled, Is.False);

            Invoke(director, "EnterBlanket");
            AssertPattern(director, "Blanket");
            Assert.That(chase.enabled, Is.True);
            Assert.That(contact.enabled, Is.False);
            Assert.That(teaCup.enabled, Is.False);
            Assert.That(restraint.enabled, Is.True);

            Invoke(director, "EnterRedString");
            AssertPattern(director, "RedString");
            Assert.That(chase.enabled, Is.True);
            Assert.That(contact.enabled, Is.True);
            Assert.That(teaCup.enabled, Is.False);
            Assert.That(restraint.enabled, Is.False);

            Invoke(director, "EnterRecovery");
            AssertPattern(director, "Recovery");
            Assert.That(chase.enabled, Is.False);
            Assert.That(contact.enabled, Is.False);
            Assert.That(teaCup.enabled, Is.False);
            Assert.That(restraint.enabled, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(grandma);
        }
    }

    private static GameObject LoadGrandmaPrefab()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{GrandmaFolder}/Grandma.prefab"
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
