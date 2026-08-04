using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerDashSFXTests
{
    private static readonly Type PlayerDashType =
        Type.GetType("PlayerDash, Assembly-CSharp");

    private static readonly Type SoundManagerType =
        Type.GetType("SoundManager, Assembly-CSharp");

    private static readonly Type PlaybackProfileType =
        Type.GetType(
            "SFXPlaybackProfile, Assembly-CSharp"
        );

    private GameObject playerObject;
    private GameObject soundObject;
    private Component playerDash;
    private AudioSource sfxSource;
    private AudioClip dashClip;
    private ScriptableObject testProfile;

    [SetUp]
    public void SetUp()
    {
        Assert.That(PlayerDashType, Is.Not.Null);
        Assert.That(SoundManagerType, Is.Not.Null);
        Assert.That(PlaybackProfileType, Is.Not.Null);

        CreateSoundManager();
        CreatePlayerDash();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(playerObject);
        UnityEngine.Object.DestroyImmediate(soundObject);
        UnityEngine.Object.DestroyImmediate(testProfile);
        UnityEngine.Object.DestroyImmediate(dashClip);
    }

    [Test]
    public void TryDashPlaysSfxOnlyWhenDashStarts()
    {
        sfxSource.pitch = 1f;

        bool firstDash = (bool)Invoke(
            playerDash,
            "TryDash",
            Vector2.right
        );

        Assert.That(firstDash, Is.True);
        Assert.That(
            sfxSource.pitch,
            Is.EqualTo(1.25f).Within(0.001f)
        );

        sfxSource.pitch = 1f;
        Invoke(playerDash, "OnDisable");

        bool blockedDash = (bool)Invoke(
            playerDash,
            "TryDash",
            Vector2.up
        );

        Assert.That(blockedDash, Is.False);
        Assert.That(
            sfxSource.pitch,
            Is.EqualTo(1f).Within(0.001f)
        );
    }

    private void CreateSoundManager()
    {
        soundObject = new GameObject("Dash Test SoundManager");

        GameObject bgmObject = new("BGM");
        bgmObject.transform.SetParent(soundObject.transform);
        bgmObject.AddComponent<AudioSource>();

        GameObject sfxObject = new("SFX");
        sfxObject.transform.SetParent(soundObject.transform);
        sfxSource = sfxObject.AddComponent<AudioSource>();

        dashClip = AudioClip.Create(
            "Test Dash",
            4410,
            1,
            44100,
            false
        );

        testProfile = ScriptableObject.CreateInstance(
            PlaybackProfileType
        );
        ConfigureTestProfile();

        Component soundManager = soundObject.AddComponent(
            SoundManagerType
        );

        SerializedObject serializedSoundManager =
            new SerializedObject(soundManager);
        serializedSoundManager
            .FindProperty("sfxPlaybackProfile")
            .objectReferenceValue = testProfile;

        SerializedProperty clips =
            serializedSoundManager.FindProperty("sfxClips");
        clips.arraySize = 5;
        clips.GetArrayElementAtIndex(4)
            .objectReferenceValue = dashClip;
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
            .intValue = 1;

        SerializedProperty settings =
            serializedProfile.FindProperty("settings");
        settings.arraySize = 1;

        SerializedProperty dashSettings =
            settings.GetArrayElementAtIndex(0);
        dashSettings
            .FindPropertyRelative("sfxType")
            .enumValueIndex = 4;
        dashSettings
            .FindPropertyRelative("minPitch")
            .floatValue = 1.25f;
        dashSettings
            .FindPropertyRelative("maxPitch")
            .floatValue = 1.25f;
        dashSettings
            .FindPropertyRelative("minVolumeScale")
            .floatValue = 1f;
        dashSettings
            .FindPropertyRelative("maxVolumeScale")
            .floatValue = 1f;
        dashSettings
            .FindPropertyRelative("minRetriggerInterval")
            .floatValue = 0f;
        serializedProfile.ApplyModifiedPropertiesWithoutUndo();
    }

    private void CreatePlayerDash()
    {
        playerObject = new GameObject("Dash Test Player");
        playerObject.SetActive(false);
        playerDash = playerObject.AddComponent(PlayerDashType);

        LogAssert.Expect(
            LogType.Error,
            "PlayerDash references are not fully assigned."
        );
        Invoke(playerDash, "Awake");
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

        Assert.That(method, Is.Not.Null, methodName);
        return method.Invoke(target, arguments);
    }
}
