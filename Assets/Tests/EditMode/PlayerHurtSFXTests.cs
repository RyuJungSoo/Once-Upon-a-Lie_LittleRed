using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PlayerHurtSFXTests
{
    private const string PlaybackProfilePath =
        "Assets/Resources/SFXPlaybackProfile.asset";

    private static readonly Type PlayerMentalType =
        Type.GetType("PlayerMental, Assembly-CSharp");

    private static readonly Type PlayerLevelStatsType =
        Type.GetType("PlayerLevelStats, Assembly-CSharp");

    private static readonly Type SoundManagerType =
        Type.GetType("SoundManager, Assembly-CSharp");

    private static readonly Type PlaybackProfileType =
        Type.GetType(
            "SFXPlaybackProfile, Assembly-CSharp"
        );

    private GameObject mentalObject;
    private GameObject soundObject;
    private Component playerMental;
    private Component soundManager;
    private AudioSource sfxSource;
    private AudioClip hurtClip;
    private ScriptableObject testProfile;

    [SetUp]
    public void SetUp()
    {
        Assert.That(PlayerMentalType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);
        Assert.That(SoundManagerType, Is.Not.Null);
        Assert.That(PlaybackProfileType, Is.Not.Null);

        CreateSoundManager();
        CreatePlayerMental();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(mentalObject);
        UnityEngine.Object.DestroyImmediate(soundObject);
        UnityEngine.Object.DestroyImmediate(testProfile);
        UnityEngine.Object.DestroyImmediate(hurtClip);
    }

    [Test]
    public void SharedProfileConfiguresHurtVariation()
    {
        UnityEngine.Object profile =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                PlaybackProfilePath
            );

        Assert.That(profile, Is.Not.Null);

        SerializedObject serializedProfile =
            new SerializedObject(profile);
        SerializedProperty voicePoolSize =
            serializedProfile.FindProperty("voicePoolSize");
        SerializedProperty settings =
            serializedProfile.FindProperty("settings");

        Assert.That(
            voicePoolSize.intValue,
            Is.GreaterThanOrEqualTo(2)
        );
        Assert.That(settings.arraySize, Is.GreaterThan(0));

        SerializedProperty hurtSettings =
            FindSettings(settings, 2);

        Assert.That(hurtSettings, Is.Not.Null);
        Assert.That(
            hurtSettings
                .FindPropertyRelative("minPitch")
                .floatValue,
            Is.LessThan(1f)
        );
        Assert.That(
            hurtSettings
                .FindPropertyRelative("maxPitch")
                .floatValue,
            Is.GreaterThan(1f)
        );
        Assert.That(
            hurtSettings
                .FindPropertyRelative(
                    "minRetriggerInterval"
                )
                .floatValue,
            Is.GreaterThan(0f)
        );
    }

    [Test]
    public void NonLethalIncomingDamagePlaysHurtProfile()
    {
        sfxSource.pitch = 1f;

        Invoke(playerMental, "TakeMentalDamage", 10f);

        Assert.That(
            (float)GetProperty(
                playerMental,
                "CurrentMental"
            ),
            Is.EqualTo(90f).Within(0.001f)
        );
        Assert.That(
            sfxSource.pitch,
            Is.EqualTo(1.25f).Within(0.001f)
        );
    }

    [Test]
    public void BlockedIncomingDamageDoesNotPlayHurt()
    {
        sfxSource.pitch = 1f;
        Invoke(
            playerMental,
            "BlockIncomingMentalDamage",
            5f
        );

        Invoke(playerMental, "TakeMentalDamage", 10f);

        Assert.That(
            (float)GetProperty(
                playerMental,
                "CurrentMental"
            ),
            Is.EqualTo(100f).Within(0.001f)
        );
        Assert.That(
            sfxSource.pitch,
            Is.EqualTo(1f).Within(0.001f)
        );
    }

    [Test]
    public void RawMentalDecreaseDoesNotPlayHurt()
    {
        sfxSource.pitch = 1f;

        Invoke(playerMental, "DecreaseMentalRaw", 10f);

        Assert.That(
            (float)GetProperty(
                playerMental,
                "CurrentMental"
            ),
            Is.EqualTo(90f).Within(0.001f)
        );
        Assert.That(
            sfxSource.pitch,
            Is.EqualTo(1f).Within(0.001f)
        );
    }

    private void CreateSoundManager()
    {
        soundObject = new GameObject(
            "Test SoundManager"
        );
        GameObject sfxObject = new GameObject("SFX");
        sfxObject.transform.SetParent(soundObject.transform);
        sfxSource = sfxObject.AddComponent<AudioSource>();

        hurtClip = AudioClip.Create(
            "Test Hurt",
            4410,
            1,
            44100,
            false
        );

        testProfile = ScriptableObject.CreateInstance(
            PlaybackProfileType
        );
        ConfigureTestProfile();

        soundManager = soundObject.AddComponent(
            SoundManagerType
        );

        SerializedObject serializedSoundManager =
            new SerializedObject(soundManager);
        serializedSoundManager
            .FindProperty("sfxSource")
            .objectReferenceValue = sfxSource;
        serializedSoundManager
            .FindProperty("sfxPlaybackProfile")
            .objectReferenceValue = testProfile;

        SerializedProperty clips =
            serializedSoundManager.FindProperty("sfxClips");
        clips.arraySize = 3;
        clips.GetArrayElementAtIndex(2)
            .objectReferenceValue = hurtClip;
        serializedSoundManager
            .ApplyModifiedPropertiesWithoutUndo();

        Invoke(soundManager, "Awake");
    }

    private void ConfigureTestProfile()
    {
        SerializedObject serializedProfile =
            new SerializedObject(testProfile);
        serializedProfile
            .FindProperty("voicePoolSize")
            .intValue = 2;

        SerializedProperty settings =
            serializedProfile.FindProperty("settings");
        settings.arraySize = 1;

        SerializedProperty hurtSettings =
            settings.GetArrayElementAtIndex(0);
        hurtSettings
            .FindPropertyRelative("sfxType")
            .enumValueIndex = 2;
        hurtSettings
            .FindPropertyRelative("minPitch")
            .floatValue = 1.25f;
        hurtSettings
            .FindPropertyRelative("maxPitch")
            .floatValue = 1.25f;
        hurtSettings
            .FindPropertyRelative("minVolumeScale")
            .floatValue = 1f;
        hurtSettings
            .FindPropertyRelative("maxVolumeScale")
            .floatValue = 1f;
        hurtSettings
            .FindPropertyRelative("minRetriggerInterval")
            .floatValue = 0f;
        serializedProfile.ApplyModifiedPropertiesWithoutUndo();
    }

    private void CreatePlayerMental()
    {
        mentalObject = new GameObject(
            "Test Player Mental"
        );
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
    }

    private static SerializedProperty FindSettings(
        SerializedProperty settings,
        int sfxType
    )
    {
        for (int i = 0; i < settings.arraySize; i++)
        {
            SerializedProperty candidate =
                settings.GetArrayElementAtIndex(i);

            if (candidate
                    .FindPropertyRelative("sfxType")
                    .enumValueIndex == sfxType)
            {
                return candidate;
            }
        }

        return null;
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
