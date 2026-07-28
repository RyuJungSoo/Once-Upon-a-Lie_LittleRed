using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class ExperienceProgressSceneTests
{
    private const string Stage1ScenePath =
        "Assets/Scenes/Stage1_Scene.unity";

    private const string SourcePrefabPath =
        "Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab";

    private const string SourcePrefabGuid =
        "ad84c37a896f54e1180d600bd7746b95";

    private const string FillerSourceFileId =
        "114361196886809056";

    private const float ExpectedRed = 142f / 255f;
    private const float ExpectedGreen = 197f / 255f;
    private const float ExpectedBlue = 74f / 255f;

    [Test]
    public void Stage1ExperienceGaugeSerializedYamlMatchesContract()
    {
        string stageYaml =
            File.ReadAllText(Stage1ScenePath);
        string prefabYaml =
            File.ReadAllText(SourcePrefabPath);

        AssertSerializedReference(stageYaml, "experienceGauge");
        AssertSerializedReference(stageYaml, "levelText");

        Dictionary<string, string> overrides =
            ExtractFillerOverrides(stageYaml);

        AssertFloat(
            overrides,
            "m_Color.r",
            ExpectedRed
        );
        AssertFloat(
            overrides,
            "m_Color.g",
            ExpectedGreen
        );
        AssertFloat(
            overrides,
            "m_Color.b",
            ExpectedBlue
        );
        AssertFloat(overrides, "m_Color.a", 1f);
        AssertFloat(overrides, "m_FillAmount", 0f);

        string sourceFiller =
            ExtractYamlObjectBlock(
                prefabYaml,
                "--- !u!114 &" + FillerSourceFileId
            );

        AssertScalar(sourceFiller, "m_Type", "3");
        AssertScalar(sourceFiller, "m_FillMethod", "0");
        AssertScalar(sourceFiller, "m_FillOrigin", "0");
    }

    private static void AssertSerializedReference(
        string yaml,
        string propertyName
    )
    {
        Match match = Regex.Match(
            yaml,
            @"^\s+" + Regex.Escape(propertyName) +
            @":\s+\{fileID:\s*(-?\d+)\}",
            RegexOptions.Multiline
        );

        Assert.That(match.Success, Is.True, propertyName);
        Assert.That(
            long.Parse(
                match.Groups[1].Value,
                CultureInfo.InvariantCulture
            ),
            Is.Not.Zero,
            propertyName
        );
    }

    private static Dictionary<string, string> ExtractFillerOverrides(
        string yaml
    )
    {
        string targetPattern =
            @"-\s+target:\s+\{fileID:\s+" +
            FillerSourceFileId +
            @",\s+guid:\s+" +
            SourcePrefabGuid +
            @",\s+type:\s+3\}\s*\r?\n" +
            @"\s+propertyPath:\s+([^\r\n]+)\s*\r?\n" +
            @"\s+value:\s+([^\r\n]+)";

        MatchCollection matches = Regex.Matches(
            yaml,
            targetPattern
        );
        Assert.That(matches.Count, Is.GreaterThan(0));

        Dictionary<string, string> values =
            new Dictionary<string, string>();
        foreach (Match match in matches)
        {
            values[match.Groups[1].Value.Trim()] =
                match.Groups[2].Value.Trim();
        }

        return values;
    }

    private static string ExtractYamlObjectBlock(
        string yaml,
        string marker
    )
    {
        int start = yaml.IndexOf(
            marker,
            StringComparison.Ordinal
        );
        Assert.That(start, Is.GreaterThanOrEqualTo(0), marker);

        int next = yaml.IndexOf(
            "\n--- !u!",
            start + marker.Length,
            StringComparison.Ordinal
        );

        return next < 0
            ? yaml.Substring(start)
            : yaml.Substring(start, next - start);
    }

    private static void AssertScalar(
        string yaml,
        string propertyName,
        string expected
    )
    {
        Match match = Regex.Match(
            yaml,
            @"^\s+" + Regex.Escape(propertyName) +
            @":\s+([^\r\n]+)",
            RegexOptions.Multiline
        );

        Assert.That(match.Success, Is.True, propertyName);
        Assert.That(
            match.Groups[1].Value.Trim(),
            Is.EqualTo(expected),
            propertyName
        );
    }

    private static void AssertFloat(
        IReadOnlyDictionary<string, string> values,
        string propertyName,
        float expected
    )
    {
        Assert.That(
            values.ContainsKey(propertyName),
            Is.True,
            propertyName
        );
        Assert.That(
            float.Parse(
                values[propertyName],
                CultureInfo.InvariantCulture
            ),
            Is.EqualTo(expected).Within(0.001f),
            propertyName
        );
    }
}
