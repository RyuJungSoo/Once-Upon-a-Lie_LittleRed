using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class MentalFilmGrainEffectTests
{
    private static readonly Type FilmGrainEffectType =
        Type.GetType("MentalFilmGrainEffect, Assembly-CSharp");

    private GameObject effectObject;
    private Component effect;
    private Volume volume;
    private VolumeProfile sharedProfile;
    private VolumeProfile runtimeProfile;
    private FilmGrain sharedFilmGrain;
    private FilmGrain runtimeFilmGrain;

    [SetUp]
    public void SetUp()
    {
        Assert.That(FilmGrainEffectType, Is.Not.Null);

        effectObject =
            new GameObject("Test Mental Film Grain Effect");
        effectObject.SetActive(false);

        volume = effectObject.AddComponent<Volume>();
        sharedProfile =
            ScriptableObject.CreateInstance<VolumeProfile>();
        sharedFilmGrain = sharedProfile.Add<FilmGrain>(true);
        sharedFilmGrain.intensity.Override(0.17f);
        sharedFilmGrain.response.Override(0.8f);
        volume.sharedProfile = sharedProfile;

        runtimeProfile = volume.profile;
        effect = effectObject.AddComponent(FilmGrainEffectType);

        SetField(effect, "minIntensity", 0f);
        SetField(effect, "maxIntensity", 0.5f);
        Invoke(effect, "Initialize", runtimeProfile);

        Assert.That(
            runtimeProfile.TryGet(out runtimeFilmGrain),
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
    [TestCase(0.5f, 0.25f)]
    [TestCase(1f, 0.5f)]
    public void DangerRatioUpdatesRuntimeFilmGrainIntensity(
        float dangerRatio,
        float expectedIntensity
    )
    {
        Invoke(effect, "Apply", dangerRatio);

        Assert.That(
            runtimeFilmGrain.intensity.value,
            Is.EqualTo(expectedIntensity).Within(0.001f)
        );
        Assert.That(
            runtimeFilmGrain.intensity.overrideState,
            Is.True
        );
    }

    [Test]
    public void InitializationKeepsProfileOwnedFilmGrainSettings()
    {
        Assert.That(
            runtimeFilmGrain.response.value,
            Is.EqualTo(0.8f).Within(0.001f)
        );
        Assert.That(
            sharedFilmGrain.intensity.value,
            Is.EqualTo(0.17f).Within(0.001f)
        );
        Assert.That(
            sharedFilmGrain.response.value,
            Is.EqualTo(0.8f).Within(0.001f)
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
