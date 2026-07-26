using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class MentalVignetteEffectTests
{
    private static readonly Type VignetteEffectType =
        Type.GetType("MentalVignetteEffect, Assembly-CSharp");

    private GameObject effectObject;
    private Component effect;
    private Volume volume;
    private VolumeProfile sharedProfile;
    private VolumeProfile runtimeProfile;
    private Vignette sharedVignette;
    private Vignette runtimeVignette;

    [SetUp]
    public void SetUp()
    {
        Assert.That(VignetteEffectType, Is.Not.Null);

        effectObject =
            new GameObject("Test Mental Vignette Effect");
        effectObject.SetActive(false);

        volume = effectObject.AddComponent<Volume>();
        sharedProfile =
            ScriptableObject.CreateInstance<VolumeProfile>();
        sharedVignette = sharedProfile.Add<Vignette>(true);
        sharedVignette.intensity.Override(0.23f);
        volume.sharedProfile = sharedProfile;

        runtimeProfile = volume.profile;
        effect = effectObject.AddComponent(VignetteEffectType);

        SetField(effect, "minIntensity", 0f);
        SetField(effect, "maxIntensity", 0.88f);
        Invoke(effect, "Initialize", runtimeProfile);

        Assert.That(
            runtimeProfile.TryGet(out runtimeVignette),
            Is.True
        );
    }

    [TearDown]
    public void TearDown()
    {
        if (effectObject != null)
        {
            UnityEngine.Object.DestroyImmediate(effectObject);
        }

        if (runtimeProfile != null)
        {
            UnityEngine.Object.DestroyImmediate(runtimeProfile);
        }

        if (sharedProfile != null)
        {
            UnityEngine.Object.DestroyImmediate(sharedProfile);
        }
    }

    [TestCase(0f, 0f)]
    [TestCase(0.5f, 0.44f)]
    [TestCase(1f, 0.88f)]
    public void DangerRatioUpdatesRuntimeVignetteIntensity(
        float dangerRatio,
        float expectedIntensity
    )
    {
        Invoke(effect, "Apply", dangerRatio);

        Assert.That(
            runtimeVignette.intensity.value,
            Is.EqualTo(expectedIntensity).Within(0.001f)
        );
        Assert.That(
            runtimeVignette.intensity.overrideState,
            Is.True
        );
    }

    [Test]
    public void InitializationDoesNotModifySharedProfile()
    {
        Assert.That(
            sharedVignette.intensity.value,
            Is.EqualTo(0.23f).Within(0.001f)
        );
        Assert.That(
            runtimeVignette.intensity.value,
            Is.EqualTo(0f).Within(0.001f)
        );
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
                BindingFlags.Instance |
                BindingFlags.NonPublic
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
}
