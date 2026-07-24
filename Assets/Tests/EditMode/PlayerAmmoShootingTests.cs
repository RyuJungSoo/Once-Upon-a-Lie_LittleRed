using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerAmmoShootingTests
{
    private static readonly Type PlayerAmmoType =
        Type.GetType("PlayerAmmo, Assembly-CSharp");

    private static readonly Type PlayerLevelStatsType =
        Type.GetType("PlayerLevelStats, Assembly-CSharp");

    private static readonly Type PlayerShootingType =
        Type.GetType("PlayerShooting, Assembly-CSharp");

    private static readonly Type PlayerMovementType =
        Type.GetType("PlayerMovement, Assembly-CSharp");

    private static readonly Type BulletProjectileType =
        Type.GetType("BulletProjectile, Assembly-CSharp");

    private GameObject ammoObject;
    private Component ammo;

    [SetUp]
    public void SetUp()
    {
        Assert.That(PlayerAmmoType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);
        Assert.That(PlayerShootingType, Is.Not.Null);
        Assert.That(PlayerMovementType, Is.Not.Null);
        Assert.That(BulletProjectileType, Is.Not.Null);

        ammoObject = new GameObject("Test Player Ammo");
        ammoObject.SetActive(false);
        ammo = ammoObject.AddComponent(PlayerAmmoType);

        Component levelStats = ammoObject.GetComponent(
            PlayerLevelStatsType
        );

        Invoke(levelStats, "RecalculateStats", 1);
        Invoke(ammo, "Awake");
        Invoke(ammo, "ResetAmmo");
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(ammoObject);
    }

    [Test]
    public void TryFireConsumesAmmoThroughPlayerAmmo()
    {
        GameObject shooterObject =
            new GameObject("Test Shooter");
        GameObject bulletObject =
            new GameObject("Test Bullet");

        try
        {
            shooterObject.SetActive(false);

            Component movement = shooterObject.AddComponent(
                PlayerMovementType
            );
            Animator animator =
                shooterObject.GetComponent<Animator>();
            animator.runtimeAnimatorController =
                UnityEditor.AssetDatabase.LoadAssetAtPath
                    <RuntimeAnimatorController>(
                        "Assets/Sprites/Red/Red.controller"
                    );

            SetField(
                movement,
                "animator",
                animator
            );
            SetField(
                movement,
                "spriteRenderer",
                shooterObject.GetComponent<SpriteRenderer>()
            );

            Component shooting = shooterObject.AddComponent(
                PlayerShootingType
            );

            bulletObject.SetActive(false);
            Component bullet = bulletObject.AddComponent(
                BulletProjectileType
            );

            SetField(shooting, "playerAmmo", ammo);
            SetField(shooting, "playerMovement", movement);
            SetField(shooting, "bulletPrefab", bullet);

            int ammoBefore =
                GetProperty<int>(ammo, "CurrentAmmo");

            bool fired = Invoke<bool>(
                shooting,
                "TryFire",
                Vector2.right
            );

            Assert.That(fired, Is.True);
            Assert.That(
                GetProperty<int>(ammo, "CurrentAmmo"),
                Is.EqualTo(ammoBefore - 1)
            );
            Assert.That(
                FindTestBulletClones().Length,
                Is.EqualTo(1),
                "TryFire did not spawn a projectile."
            );
        }
        finally
        {
            foreach (Component bullet in FindTestBullets())
            {
                UnityEngine.Object.DestroyImmediate(
                    bullet.gameObject
                );
            }

            UnityEngine.Object.DestroyImmediate(shooterObject);
        }
    }

    [Test]
    public void TimedReloadRefillsMagazineAndTracksProgress()
    {
        SetField(ammo, "reloadDuration", 1f);
        Invoke(ammo, "SetAmmo", 4);

        bool started = Invoke<bool>(
            ammo,
            "TryStartReload"
        );

        Assert.That(started, Is.True);
        Assert.That(
            GetProperty<bool>(ammo, "IsReloading"),
            Is.True
        );

        Invoke(ammo, "AdvanceReload", 0.25f);

        Assert.That(
            GetProperty<float>(ammo, "ReloadProgress"),
            Is.EqualTo(0.25f).Within(0.001f)
        );
        Assert.That(
            GetProperty<int>(ammo, "CurrentAmmo"),
            Is.EqualTo(4)
        );
        Assert.That(
            Invoke<bool>(ammo, "TryUseAmmo", 1),
            Is.False,
            "Ammo must not be consumed while reloading."
        );

        Invoke(ammo, "AdvanceReload", 0.75f);

        Assert.That(
            GetProperty<bool>(ammo, "IsReloading"),
            Is.False
        );
        Assert.That(
            GetProperty<float>(ammo, "ReloadProgress"),
            Is.EqualTo(0f)
        );
        Assert.That(
            GetProperty<int>(ammo, "CurrentAmmo"),
            Is.EqualTo(GetProperty<int>(ammo, "MaxAmmo"))
        );
    }

    private static Component[] FindTestBullets()
    {
        return Resources
            .FindObjectsOfTypeAll(BulletProjectileType)
            .OfType<Component>()
            .Where(component =>
                component.gameObject.scene.IsValid() &&
                component.gameObject.name.StartsWith(
                    "Test Bullet",
                    StringComparison.Ordinal
                ))
            .ToArray();
    }

    private static Component[] FindTestBulletClones()
    {
        return FindTestBullets()
            .Where(component =>
                component.gameObject.name ==
                "Test Bullet(Clone)")
            .ToArray();
    }

    private static T GetProperty<T>(
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
        return (T)property.GetValue(target);
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

    private static T Invoke<T>(
        object target,
        string methodName,
        params object[] arguments
    )
    {
        return (T)Invoke(
            target,
            methodName,
            arguments
        );
    }
}
