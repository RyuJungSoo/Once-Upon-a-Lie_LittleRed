using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerBulletPenetrationTests
{
    private static readonly Type BulletProjectileType =
        Type.GetType("BulletProjectile, Assembly-CSharp");
    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");
    private static readonly Type PlayerLevelStatsType =
        Type.GetType("PlayerLevelStats, Assembly-CSharp");

    [Test]
    public void LaunchUsesPlayerLevelStatsForBulletRuntimeValues()
    {
        Assert.That(BulletProjectileType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);

        GameObject statsObject = new GameObject("Bullet Test Stats");
        GameObject bulletObject = new GameObject("Bullet Test Projectile");

        try
        {
            Component stats = statsObject.AddComponent(
                PlayerLevelStatsType
            );
            Component bullet = bulletObject.AddComponent(
                BulletProjectileType
            );
            Rigidbody2D body = bulletObject.GetComponent<Rigidbody2D>();

            Invoke(stats, "RecalculateStats", 5);
            Invoke(bullet, "Launch", Vector2.right, stats);

            Assert.That(
                body.linearVelocity,
                Is.EqualTo(Vector2.right * 11f)
            );
            Assert.That(
                GetField<int>(bullet, "damage"),
                Is.EqualTo(14)
            );
            Assert.That(
                GetField<int>(bullet, "remainingPenetration"),
                Is.EqualTo(1)
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bulletObject);
            UnityEngine.Object.DestroyImmediate(statsObject);
        }
    }

    [Test]
    public void LevelFiveBulletPassesFirstMonster()
    {
        Assert.That(BulletProjectileType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);

        GameObject statsObject = new GameObject("Bullet Test Stats");
        GameObject bulletObject = new GameObject("Bullet Test Projectile");
        GameObject firstMonster = CreateMonster(
            "First Monster",
            out ScriptableObject firstMonsterStats
        );

        try
        {
            Component stats = statsObject.AddComponent(
                PlayerLevelStatsType
            );
            Component bullet = bulletObject.AddComponent(
                BulletProjectileType
            );

            Invoke(stats, "RecalculateStats", 5);
            Invoke(bullet, "Launch", Vector2.right, stats);

            Invoke(
                bullet,
                "OnTriggerEnter2D",
                firstMonster.GetComponent<BoxCollider2D>()
            );

            Assert.That(
                GetProperty<bool>(
                    firstMonster.GetComponent(MonsterHealthType),
                    "IsDead"
                ),
                Is.False
            );
            Assert.That(
                GetProperty<int>(
                    firstMonster.GetComponent(MonsterHealthType),
                    "CurrentHealth"
                ),
                Is.EqualTo(86)
            );
            Assert.That(
                GetField<bool>(bullet, "isSpent"),
                Is.False
            );
            Assert.That(
                GetField<int>(bullet, "remainingPenetration"),
                Is.EqualTo(0)
            );
        }
        finally
        {
            if (bulletObject != null)
            {
                UnityEngine.Object.DestroyImmediate(bulletObject);
            }

            if (firstMonster != null)
            {
                UnityEngine.Object.DestroyImmediate(firstMonster);
            }

            UnityEngine.Object.DestroyImmediate(firstMonsterStats);
            UnityEngine.Object.DestroyImmediate(statsObject);
        }
    }

    private static GameObject CreateMonster(
        string name,
        out ScriptableObject stats
    )
    {
        stats = ScriptableObject.CreateInstance("MonsterStats");
        SetField(stats, "maxHealth", 100);

        GameObject monster = new GameObject(name);
        monster.AddComponent<BoxCollider2D>();
        Component health = monster.AddComponent(MonsterHealthType);
        SetField(health, "stats", stats);
        Invoke(health, "OnEnable");
        return monster;
    }

    private static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }

    private static T GetProperty<T>(
        object target,
        string propertyName
    )
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public
        );

        Assert.That(property, Is.Not.Null);
        return (T)property.GetValue(target);
    }

    private static void SetField(
        object target,
        string fieldName,
        object value
    )
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static object Invoke(
        object target,
        string methodName,
        params object[] arguments
    )
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        Assert.That(method, Is.Not.Null);
        return method.Invoke(target, arguments);
    }
}
