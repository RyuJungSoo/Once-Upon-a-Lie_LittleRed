using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MonsterRangedAttackTests
{
    private static readonly Type MonsterAttackType =
        Type.GetType("MonsterAttack, Assembly-CSharp");
    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");
    private static readonly Type MonsterProjectileType =
        Type.GetType("MonsterProjectile, Assembly-CSharp");
    private static readonly Type MonsterRangedAttackType =
        Type.GetType("MonsterRangedAttack, Assembly-CSharp");
    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    [Test]
    public void FlowerFairyUsesMobBulletAndContactAttack()
    {
        Assert.That(MonsterAttackType, Is.Not.Null);
        Assert.That(MonsterChaseType, Is.Not.Null);
        Assert.That(MonsterProjectileType, Is.Not.Null);
        Assert.That(MonsterRangedAttackType, Is.Not.Null);

        GameObject flowerFairy =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Monster/FlowerFairy/FlowerFairy.prefab"
            );
        GameObject mobBullet =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Mob_Bullet1.prefab"
            );

        Assert.That(flowerFairy, Is.Not.Null);
        Assert.That(mobBullet, Is.Not.Null);
        Assert.That(
            flowerFairy.GetComponent(MonsterAttackType),
            Is.Not.Null
        );
        Assert.That(
            flowerFairy.GetComponent(MonsterChaseType),
            Is.Not.Null
        );

        Component rangedAttack =
            flowerFairy.GetComponent(MonsterRangedAttackType);
        Component projectile =
            mobBullet.GetComponent(MonsterProjectileType);

        Assert.That(rangedAttack, Is.Not.Null);
        Assert.That(projectile, Is.Not.Null);
        Assert.That(mobBullet.GetComponent<Rigidbody2D>(), Is.Not.Null);

        CircleCollider2D projectileCollider =
            mobBullet.GetComponent<CircleCollider2D>();

        Assert.That(projectileCollider, Is.Not.Null);
        Assert.That(projectileCollider.isTrigger, Is.True);

        Component health =
            flowerFairy.GetComponent(MonsterHealthType);
        object stats = GetProperty(health, "Stats");
        object settings = GetProperty(
            stats,
            "RangedAttack"
        );

        Assert.That(
            GetProperty(settings, "ProjectilePrefab"),
            Is.SameAs(projectile)
        );
        Assert.That(
            (float)GetProperty(settings, "AttackRange"),
            Is.GreaterThan(0f)
        );
        Assert.That(
            (float)GetProperty(settings, "ProjectileSpeed"),
            Is.GreaterThan(0f)
        );
        Assert.That(
            (float)GetProperty(settings, "ProjectileDamage"),
            Is.GreaterThan(0f)
        );

        SerializedObject serializedAttack =
            new SerializedObject(rangedAttack);

        Assert.That(
            serializedAttack.FindProperty("projectilePrefab"),
            Is.Null
        );
    }

    [Test]
    public void RangedAttackStopsAndResumesChaseAtConfiguredRanges()
    {
        Assert.That(MonsterChaseType, Is.Not.Null);
        Assert.That(MonsterRangedAttackType, Is.Not.Null);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Sprites/Monster/FlowerFairy/FlowerFairy.prefab"
        );
        GameObject monster = UnityEngine.Object.Instantiate(prefab);
        GameObject player = new GameObject("Ranged Attack Test Player");

        try
        {
            Behaviour chase =
                (Behaviour)monster.GetComponent(MonsterChaseType);
            Component rangedAttack =
                monster.GetComponent(MonsterRangedAttackType);
            Component health =
                monster.GetComponent(MonsterHealthType);
            object stats = GetProperty(health, "Stats");
            object settings = GetProperty(
                stats,
                "RangedAttack"
            );
            float attackRange = (float)GetProperty(
                settings,
                "AttackRange"
            );
            float resumePadding = (float)GetProperty(
                settings,
                "ResumeRangePadding"
            );

            Invoke(rangedAttack, "Awake");
            SetField(chase, "target", player.transform);

            player.transform.position =
                monster.transform.position +
                Vector3.right * (attackRange - 0.1f);

            Invoke(rangedAttack, "Update");

            Assert.That(chase.enabled, Is.False);

            player.transform.position =
                monster.transform.position +
                Vector3.right *
                (attackRange + resumePadding + 0.1f);

            Invoke(rangedAttack, "Update");

            Assert.That(chase.enabled, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(monster);
        }
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
                BindingFlags.Instance | BindingFlags.NonPublic
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
