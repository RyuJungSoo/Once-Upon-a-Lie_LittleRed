using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class MentalCameraShakeTests
{
    private static readonly Type GameManagerType =
        Type.GetType("GameManager, Assembly-CSharp");

    private static readonly Type PlayerExperienceType =
        Type.GetType("PlayerExperience, Assembly-CSharp");

    private static readonly Type PlayerLevelStatsType =
        Type.GetType("PlayerLevelStats, Assembly-CSharp");

    private static readonly Type PlayerMentalType =
        Type.GetType("PlayerMental, Assembly-CSharp");

    private static readonly Type MentalCameraShakeType =
        Type.GetType("MentalCameraShake, Assembly-CSharp");

    private static readonly Type CinemachineCameraType =
        Type.GetType(
            "Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine"
        );

    private static readonly Type CinemachineNoiseType =
        Type.GetType(
            "Unity.Cinemachine.CinemachineBasicMultiChannelPerlin, " +
            "Unity.Cinemachine"
        );

    private GameObject managerObject;
    private GameObject duplicateManagerObject;
    private GameObject cameraObject;
    private Component persistentMental;
    private Component cameraShake;
    private Component noise;

    [SetUp]
    public void SetUp()
    {
        Assert.That(GameManagerType, Is.Not.Null);
        Assert.That(PlayerExperienceType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);
        Assert.That(PlayerMentalType, Is.Not.Null);
        Assert.That(MentalCameraShakeType, Is.Not.Null);
        Assert.That(CinemachineCameraType, Is.Not.Null);
        Assert.That(CinemachineNoiseType, Is.Not.Null);

        CreatePersistentGameManager();
        CreateDuplicateSceneMental();
        CreateRecreatedCamera();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(cameraObject);
        UnityEngine.Object.DestroyImmediate(duplicateManagerObject);
        UnityEngine.Object.DestroyImmediate(managerObject);
    }

    [Test]
    public void RecreatedCameraBindsPersistentMentalAndClearsShake()
    {
        Invoke(cameraShake, "Awake");
        Invoke(cameraShake, "Start");

        Assert.That(
            GetField<Component>(cameraShake, "playerMental"),
            Is.SameAs(persistentMental)
        );
        Assert.That(
            GetFloatField(noise, "AmplitudeGain"),
            Is.EqualTo(0f).Within(0.001f)
        );
        Assert.That(
            GetField<float>(cameraShake, "targetAmplitude"),
            Is.EqualTo(0f).Within(0.001f)
        );
    }

    private void CreatePersistentGameManager()
    {
        managerObject = new GameObject("Persistent GameManager");
        managerObject.SetActive(false);

        Component levelStats =
            managerObject.AddComponent(PlayerLevelStatsType);
        persistentMental =
            managerObject.AddComponent(PlayerMentalType);
        Component playerExperience =
            managerObject.AddComponent(PlayerExperienceType);
        Component gameManager =
            managerObject.AddComponent(GameManagerType);

        Invoke(levelStats, "RecalculateStats", 1);
        Invoke(persistentMental, "Awake");
        Invoke(persistentMental, "ResetMental");
        Invoke(playerExperience, "Awake");
        Invoke(gameManager, "Awake");
    }

    private void CreateDuplicateSceneMental()
    {
        duplicateManagerObject =
            new GameObject("Duplicate Scene GameManager");
        duplicateManagerObject.SetActive(false);
        duplicateManagerObject.AddComponent(PlayerLevelStatsType);
        duplicateManagerObject.AddComponent(PlayerMentalType);
    }

    private void CreateRecreatedCamera()
    {
        cameraObject = new GameObject("Recreated Cinemachine Camera");
        cameraObject.SetActive(false);
        cameraObject.AddComponent(CinemachineCameraType);
        noise = cameraObject.AddComponent(CinemachineNoiseType);
        cameraShake = cameraObject.AddComponent(MentalCameraShakeType);

        Component duplicateMental =
            duplicateManagerObject.GetComponent(PlayerMentalType);
        SetField(cameraShake, "playerMental", duplicateMental);
        SetField(cameraShake, "highAmplitude", 0f);
        SetField(cameraShake, "lowAmplitude", 1f);
    }

    private static float GetFloatField(
        Component component,
        string fieldName
    )
    {
        FieldInfo field = component.GetType().GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.Public
        );

        Assert.That(field, Is.Not.Null, fieldName);
        return (float)field.GetValue(component);
    }

    private static void SetField(
        object target,
        string fieldName,
        object value
    )
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static T GetField<T>(
        object target,
        string fieldName
    )
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
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

