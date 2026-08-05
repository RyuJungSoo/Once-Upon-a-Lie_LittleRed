using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MonsterProjectileTests
{
    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");
    private static readonly Type MonsterProjectileType =
        Type.GetType("MonsterProjectile, Assembly-CSharp");
    private static readonly Type PlayerLevelStatsType =
        Type.GetType("PlayerLevelStats, Assembly-CSharp");
    private static readonly Type PlayerMentalType =
        Type.GetType("PlayerMental, Assembly-CSharp");

    [Test]
    public void MobBullet2MatchesMobBullet1CollisionPhysics()
    {
        GameObject referencePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Mob_Bullet1.prefab"
            );
        GameObject teaCupProjectilePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Mob_Bullet2.prefab"
            );

        Assert.That(referencePrefab, Is.Not.Null);
        Assert.That(teaCupProjectilePrefab, Is.Not.Null);

        Rigidbody2D referenceBody =
            referencePrefab.GetComponent<Rigidbody2D>();
        Rigidbody2D teaCupProjectileBody =
            teaCupProjectilePrefab.GetComponent<Rigidbody2D>();
        Collider2D referenceCollider =
            referencePrefab.GetComponent<Collider2D>();
        Collider2D teaCupProjectileCollider =
            teaCupProjectilePrefab.GetComponent<Collider2D>();

        Assert.That(
            teaCupProjectileCollider.isTrigger,
            Is.EqualTo(referenceCollider.isTrigger)
        );
        Assert.That(
            teaCupProjectileBody.gravityScale,
            Is.EqualTo(referenceBody.gravityScale)
        );
        Assert.That(
            teaCupProjectileBody.constraints,
            Is.EqualTo(referenceBody.constraints)
        );
        Assert.That(
            teaCupProjectileBody.collisionDetectionMode,
            Is.EqualTo(referenceBody.collisionDetectionMode)
        );
        Assert.That(
            teaCupProjectileBody.interpolation,
            Is.EqualTo(referenceBody.interpolation)
        );
    }

    [Test]
    public void PlayerTagCausesDamageAndProjectileDestruction()
    {
        Assert.That(MonsterProjectileType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);
        Assert.That(PlayerMentalType, Is.Not.Null);

        GameObject bullet = CreateLaunchedBullet(12f);
        Component projectile =
            bullet.GetComponent(MonsterProjectileType);
        GameObject player = new GameObject("Projectile Test Player");
        player.SetActive(false);

        BoxCollider2D playerCollider =
            player.AddComponent<BoxCollider2D>();
        Component playerMental =
            player.AddComponent(PlayerMentalType);
        Component levelStats =
            player.GetComponent(PlayerLevelStatsType);

        try
        {
            Invoke(levelStats, "RecalculateStats", 1);
            player.SetActive(true);
            Invoke(playerMental, "ResetMental");

            Invoke(
                projectile,
                "OnTriggerEnter2D",
                playerCollider
            );

            Assert.That(bullet, Is.Not.Null);
            Assert.That(
                (float)GetProperty(
                    playerMental,
                    "CurrentMental"
                ),
                Is.EqualTo(100f).Within(0.001f)
            );

            player.tag = "Player";

            Invoke(
                projectile,
                "OnTriggerEnter2D",
                playerCollider
            );

            Assert.That(bullet == null, Is.True);
            Assert.That(
                (float)GetProperty(
                    playerMental,
                    "CurrentMental"
                ),
                Is.EqualTo(88f).Within(0.001f)
            );
        }
        finally
        {
            if (bullet != null)
            {
                UnityEngine.Object.DestroyImmediate(bullet);
            }

            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [TestCase("Assets/Sprites/Bullet/Mob_Bullet1.prefab")]
    [TestCase("Assets/Sprites/Bullet/Mob_Bullet2.prefab")]
    [TestCase("Assets/Sprites/Bullet/Boss_Bullet.prefab")]
    public void PlayerBulletCollisionDestroysMonsterProjectileWithoutDamagingPlayer(
        string monsterBulletPath
    )
    {
        Assert.That(MonsterProjectileType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);
        Assert.That(PlayerMentalType, Is.Not.Null);

        GameObject monsterBullet = CreateLaunchedBullet(
            12f,
            monsterBulletPath
        );
        Component projectile =
            monsterBullet.GetComponent(MonsterProjectileType);
        GameObject playerBulletPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Bullet/Bullet.prefab"
            );
        GameObject playerBullet =
            UnityEngine.Object.Instantiate(playerBulletPrefab);
        GameObject player = new GameObject("Projectile Test Player");
        player.SetActive(false);

        Component playerMental =
            player.AddComponent(PlayerMentalType);
        Component levelStats =
            player.GetComponent(PlayerLevelStatsType);

        try
        {
            Invoke(levelStats, "RecalculateStats", 1);
            player.SetActive(true);
            Invoke(playerMental, "ResetMental");

            Invoke(
                projectile,
                "OnTriggerEnter2D",
                playerBullet.GetComponent<Collider2D>()
            );

            Assert.That(
                (float)GetProperty(
                    playerMental,
                    "CurrentMental"
                ),
                Is.EqualTo(100f).Within(0.001f)
            );
            Assert.That(monsterBullet == null, Is.True);
        }
        finally
        {
            if (monsterBullet != null)
            {
                UnityEngine.Object.DestroyImmediate(monsterBullet);
            }

            UnityEngine.Object.DestroyImmediate(playerBullet);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void MonsterCollisionDoesNotDamageOrDestroyProjectile()
    {
        Assert.That(MonsterHealthType, Is.Not.Null);
        Assert.That(MonsterProjectileType, Is.Not.Null);

        GameObject bullet = CreateLaunchedBullet(12f);
        Component projectile =
            bullet.GetComponent(MonsterProjectileType);
        GameObject monster = new GameObject(
            "Projectile Test Monster"
        );
        monster.SetActive(false);

        BoxCollider2D monsterCollider =
            monster.AddComponent<BoxCollider2D>();
        Component monsterHealth =
            monster.AddComponent(MonsterHealthType);

        try
        {
            monster.SetActive(true);
            Invoke(monsterHealth, "OnEnable");
            int healthBefore = (int)GetProperty(
                monsterHealth,
                "CurrentHealth"
            );

            Invoke(
                projectile,
                "OnTriggerEnter2D",
                monsterCollider
            );

            Assert.That(bullet, Is.Not.Null);
            Assert.That(
                (int)GetProperty(
                    monsterHealth,
                    "CurrentHealth"
                ),
                Is.EqualTo(healthBefore)
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bullet);
            UnityEngine.Object.DestroyImmediate(monster);
        }
    }

    private static GameObject CreateLaunchedBullet(
        float damage,
        string prefabPath =
            "Assets/Sprites/Bullet/Mob_Bullet1.prefab"
    )
    {
        GameObject bulletPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath
            );
        GameObject bullet =
            UnityEngine.Object.Instantiate(bulletPrefab);
        Component projectile =
            bullet.GetComponent(MonsterProjectileType);

        Invoke(projectile, "Awake");
        Invoke(
            projectile,
            "Launch",
            Vector2.right,
            6f,
            damage,
            3f
        );

        Assert.That(
            bullet.GetComponent<Rigidbody2D>().linearVelocity,
            Is.EqualTo(Vector2.right * 6f)
        );

        return bullet;
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
                BindingFlags.Instance | BindingFlags.Public
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
