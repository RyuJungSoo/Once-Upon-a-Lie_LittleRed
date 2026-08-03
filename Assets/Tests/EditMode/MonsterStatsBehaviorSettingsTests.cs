using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MonsterStatsBehaviorSettingsTests
{
    private static readonly Type MonsterStatsType =
        Type.GetType("MonsterStats, Assembly-CSharp");
    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");
    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");
    private static readonly Type MonsterAttackType =
        Type.GetType("MonsterAttack, Assembly-CSharp");
    private static readonly Type MonsterRangedAttackType =
        Type.GetType(
            "MonsterRangedAttack, Assembly-CSharp"
        );
    private static readonly Type MonsterProjectileType =
        Type.GetType("MonsterProjectile, Assembly-CSharp");
    private static readonly Type SignAttackType =
        Type.GetType("SignZigzagAttack, Assembly-CSharp");
    private static readonly Type MothFanAttackType =
        Type.GetType("MothFanAttack, Assembly-CSharp");
    private static readonly Type TeaCupBarrageAttackType =
        Type.GetType(
            "TeaCupBarrageAttack, Assembly-CSharp"
        );

    private static readonly string[] MonsterNames =
    {
        "Bird",
        "Blanket",
        "DeerKing",
        "FlowerFairy",
        "Grandma",
        "Hunter",
        "Moth",
        "Pig",
        "Rabbit",
        "RedString",
        "Sign",
        "TeaCup"
    };

    private static readonly string[] ChasingMonsterNames =
    {
        "Bird",
        "Blanket",
        "DeerKing",
        "FlowerFairy",
        "Grandma",
        "Hunter",
        "Pig",
        "Rabbit",
        "RedString",
        "Sign",
        "TeaCup"
    };

    private static readonly string[] ContactMonsterNames =
    {
        "Bird",
        "DeerKing",
        "FlowerFairy",
        "Grandma",
        "Hunter",
        "Rabbit",
        "RedString",
        "TeaCup"
    };

    [SetUp]
    public void SetUp()
    {
        Assert.That(MonsterStatsType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);
        Assert.That(MonsterChaseType, Is.Not.Null);
        Assert.That(MonsterAttackType, Is.Not.Null);
        Assert.That(MonsterRangedAttackType, Is.Not.Null);
        Assert.That(MonsterProjectileType, Is.Not.Null);
        Assert.That(SignAttackType, Is.Not.Null);
        Assert.That(MothFanAttackType, Is.Not.Null);
        Assert.That(TeaCupBarrageAttackType, Is.Not.Null);
    }

    [Test]
    public void EveryMonsterUsesItsOwnValidStatsAsset()
    {
        string[] statsGuids = AssetDatabase.FindAssets(
            "t:MonsterStats",
            new[] { "Assets/Sprites/Monster" }
        );

        Assert.That(statsGuids, Has.Length.EqualTo(12));

        foreach (string monsterName in MonsterNames)
        {
            GameObject prefab = LoadPrefab(monsterName);
            UnityEngine.Object expectedStats =
                LoadStats(monsterName);
            Component health =
                prefab.GetComponent(MonsterHealthType);

            Assert.That(health, Is.Not.Null);
            Assert.That(
                GetProperty<UnityEngine.Object>(
                    health,
                    "Stats"
                ),
                Is.SameAs(expectedStats)
            );

            object chase = GetProperty(
                expectedStats,
                "Chase"
            );
            object contact = GetProperty(
                expectedStats,
                "ContactAttack"
            );
            object ranged = GetProperty(
                expectedStats,
                "RangedAttack"
            );

            Assert.That(
                GetProperty<float>(chase, "StopDistance"),
                Is.EqualTo(0.1f)
            );
            Assert.That(
                GetProperty<float>(
                    contact,
                    "AttackCooldown"
                ),
                Is.EqualTo(0.75f)
            );
            Assert.That(
                GetProperty<float>(
                    contact,
                    "AttackAnimationDuration"
                ),
                Is.EqualTo(0.25f)
            );
            Assert.That(
                GetProperty<float>(ranged, "AttackRange"),
                Is.GreaterThan(0f)
            );
            Assert.That(
                GetProperty<float>(
                    ranged,
                    "ProjectileSpeed"
                ),
                Is.GreaterThan(0f)
            );
            Assert.That(
                GetProperty<float>(
                    ranged,
                    "ProjectileLifetime"
                ),
                Is.GreaterThan(0f)
            );
        }
    }

    [Test]
    public void SharedBehaviorComponentsReadMonsterStats()
    {
        foreach (string monsterName in ChasingMonsterNames)
        {
            GameObject prefab = LoadPrefab(monsterName);
            Component chase =
                prefab.GetComponent(MonsterChaseType);
            object statsChase = GetProperty(
                LoadStats(monsterName),
                "Chase"
            );

            Assert.That(chase, Is.Not.Null);
            Assert.That(
                GetProperty<float>(chase, "StopDistance"),
                Is.EqualTo(
                    GetProperty<float>(
                        statsChase,
                        "StopDistance"
                    )
                )
            );
            Assert.That(
                new SerializedObject(chase)
                    .FindProperty("stopDistance"),
                Is.Null
            );
        }

        foreach (string monsterName in ContactMonsterNames)
        {
            GameObject prefab = LoadPrefab(monsterName);
            Component attack =
                prefab.GetComponent(MonsterAttackType);
            object contact = GetProperty(
                LoadStats(monsterName),
                "ContactAttack"
            );

            Assert.That(attack, Is.Not.Null);
            AssertContactSettingsMatch(attack, contact);

            SerializedObject serializedAttack =
                new SerializedObject(attack);

            Assert.That(
                serializedAttack.FindProperty(
                    "attackCooldown"
                ),
                Is.Null
            );
            Assert.That(
                serializedAttack.FindProperty(
                    "attackAnimationDuration"
                ),
                Is.Null
            );
        }

        GameObject signPrefab = LoadPrefab("Sign");
        Component signAttack =
            signPrefab.GetComponent(SignAttackType);
        object signContact = GetProperty(
            LoadStats("Sign"),
            "ContactAttack"
        );

        Assert.That(signAttack, Is.Not.Null);
        AssertContactSettingsMatch(
            signAttack,
            signContact
        );
    }

    [Test]
    public void RangedMonstersUseTheirConfiguredBulletAndOthersStayDisabled()
    {
        GameObject mobBulletPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Mob_Bullet1.prefab"
            );
        GameObject bossBulletPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Boss_Bullet.prefab"
            );
        GameObject teaCupBulletPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Mob_Bullet2.prefab"
            );
        Component mobProjectile =
            mobBulletPrefab.GetComponent(MonsterProjectileType);
        Component bossProjectile =
            bossBulletPrefab.GetComponent(MonsterProjectileType);
        Component teaCupProjectile =
            teaCupBulletPrefab.GetComponent(
                MonsterProjectileType
            );

        foreach ((string monsterName, Component projectile) in new[]
                 {
                     ("FlowerFairy", mobProjectile),
                     ("DeerKing", bossProjectile),
                     ("Grandma", mobProjectile),
                     ("Moth", mobProjectile),
                     ("Hunter", mobProjectile),
                     ("TeaCup", teaCupProjectile)
                 })
        {
            object settings = GetProperty(
                LoadStats(monsterName),
                "RangedAttack"
            );

            Assert.That(
                GetProperty<float>(settings, "AttackRange"),
                Is.EqualTo(5f)
            );
            Assert.That(
                GetProperty<float>(
                    settings,
                    "ResumeRangePadding"
                ),
                Is.EqualTo(0.5f)
            );
            Assert.That(
                GetProperty<float>(
                    settings,
                    "AttackCooldown"
                ),
                Is.EqualTo(1.25f)
            );
            Assert.That(
                GetProperty<float>(
                    settings,
                    "AttackAnimationDuration"
                ),
                Is.EqualTo(0.25f)
            );
            Assert.That(
                GetProperty<UnityEngine.Object>(
                    settings,
                    "ProjectilePrefab"
                ),
                Is.SameAs(projectile)
            );
            Assert.That(
                GetProperty<Vector2>(
                    settings,
                    "ProjectileSpawnOffset"
                ),
                Is.EqualTo(new Vector2(0f, 0.2f))
            );
            Assert.That(
                GetProperty<float>(
                    settings,
                    "ProjectileSpeed"
                ),
                Is.EqualTo(7f)
            );
            Assert.That(
                GetProperty<float>(
                    settings,
                    "ProjectileDamage"
                ),
                Is.EqualTo(10f)
            );
            Assert.That(
                GetProperty<float>(
                    settings,
                    "ProjectileLifetime"
                ),
                Is.EqualTo(4f)
            );
        }

        foreach (string monsterName in new[]
                 {
                     "FlowerFairy",
                     "DeerKing"
                 })
        {
            Assert.That(
                LoadPrefab(monsterName)
                    .GetComponent(MonsterRangedAttackType),
                Is.Not.Null
            );
        }

        foreach (string monsterName in MonsterNames)
        {
            if (monsterName == "FlowerFairy" ||
                monsterName == "DeerKing" ||
                monsterName == "Grandma" ||
                monsterName == "Moth" ||
                monsterName == "Hunter" ||
                monsterName == "TeaCup")
            {
                continue;
            }

            object ranged = GetProperty(
                LoadStats(monsterName),
                "RangedAttack"
            );

            Assert.That(
                GetProperty<UnityEngine.Object>(
                    ranged,
                    "ProjectilePrefab"
                ),
                Is.Null,
                $"{monsterName} should not fire a projectile yet."
            );
            Assert.That(
                LoadPrefab(monsterName)
                    .GetComponent(MonsterRangedAttackType),
                Is.Null,
                $"{monsterName} should not have ranged behavior yet."
            );
        }
    }

    [Test]
    public void MothUsesItsConfiguredFanAttackComposition()
    {
        GameObject moth = LoadPrefab("Moth");

        Assert.That(
            moth.GetComponent<Rigidbody2D>(),
            Is.Not.Null
        );
        Assert.That(
            moth.GetComponent(MonsterChaseType),
            Is.Not.Null
        );
        Assert.That(
            moth.GetComponent(MonsterAttackType),
            Is.Not.Null
        );
        Assert.That(
            moth.GetComponent(MothFanAttackType),
            Is.Not.Null
        );
        Assert.That(
            moth.GetComponent(MonsterRangedAttackType),
            Is.Null
        );
    }

    [Test]
    public void TeaCupUsesItsConfiguredBarrageAttackComposition()
    {
        GameObject teaCup = LoadPrefab("TeaCup");

        Assert.That(
            teaCup.GetComponent<Rigidbody2D>(),
            Is.Not.Null
        );
        Assert.That(
            teaCup.GetComponent(MonsterChaseType),
            Is.Not.Null
        );
        Assert.That(
            teaCup.GetComponent(MonsterAttackType),
            Is.Not.Null
        );
        Assert.That(
            teaCup.GetComponent(TeaCupBarrageAttackType),
            Is.Not.Null
        );
        Assert.That(
            teaCup.GetComponent(MonsterRangedAttackType),
            Is.Null
        );
    }

    private static void AssertContactSettingsMatch(
        object behavior,
        object contact
    )
    {
        Assert.That(
            GetProperty<float>(
                behavior,
                "AttackCooldown"
            ),
            Is.EqualTo(
                GetProperty<float>(
                    contact,
                    "AttackCooldown"
                )
            )
        );
        Assert.That(
            GetProperty<float>(
                behavior,
                "AttackAnimationDuration"
            ),
            Is.EqualTo(
                GetProperty<float>(
                    contact,
                    "AttackAnimationDuration"
                )
            )
        );
    }

    private static GameObject LoadPrefab(string monsterName)
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Sprites/Monster/{monsterName}/" +
                $"{monsterName}.prefab"
            );

        Assert.That(prefab, Is.Not.Null);
        return prefab;
    }

    private static UnityEngine.Object LoadStats(
        string monsterName
    )
    {
        UnityEngine.Object stats =
            AssetDatabase.LoadAssetAtPath(
                $"Assets/Sprites/Monster/{monsterName}/" +
                $"{monsterName}Stats.asset",
                MonsterStatsType
            );

        Assert.That(stats, Is.Not.Null);
        return stats;
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
}
