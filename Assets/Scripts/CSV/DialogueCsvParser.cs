using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueCsvParser : MonoBehaviour
{
    [Header("CSV")]
    [SerializeField] private TextAsset csvFile;

    [Header("Language")]
    [SerializeField] private string defaultLanguage = "ko";

    [Header("Parsing")]
    [SerializeField] private bool parseOnAwake = true;
    [SerializeField] private bool showParseLog = true;

    [Header("Parsed Entries")]
    [SerializeField]
    private List<DialogueEntryData> parsedEntries =
        new List<DialogueEntryData>();

    private readonly Dictionary<string, DialogueEntryData> entryById =
        new Dictionary<string, DialogueEntryData>();

    private readonly Dictionary<string, List<DialogueEntryData>> entriesByGroup =
        new Dictionary<string, List<DialogueEntryData>>();

    private static readonly string[] GroupHeaderAliases =
    {
        "group_id",
        "cutscene_id",
        "scene_id"
    };

    public IReadOnlyList<DialogueEntryData> ParsedEntries =>
        parsedEntries;

    public string DefaultLanguage =>
        defaultLanguage;

    public bool IsParsed { get; private set; }

    private void Awake()
    {
        if (parseOnAwake)
        {
            Parse();
        }
        else
        {
            // 에디터에서 미리 파싱해 둔 리스트가 있다면
            // 런타임 검색용 Dictionary만 다시 구성합니다.
            BuildIndexes();
        }
    }

    /// <summary>
    /// CSV 파일을 읽고 Parsed Entries 리스트를 생성합니다.
    /// </summary>
    public bool Parse()
    {
        ClearDataInternal();

        if (csvFile == null)
        {
            Debug.LogError(
                $"[{nameof(DialogueCsvParser)}] " +
                "CSV 파일이 연결되지 않았습니다.",
                this
            );

            return false;
        }

        string normalizedCsv = csvFile.text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        string[] lines = normalizedCsv.Split('\n');

        if (lines.Length <= 1)
        {
            Debug.LogError(
                $"[{nameof(DialogueCsvParser)}] " +
                "CSV에 데이터 행이 없습니다.",
                this
            );

            return false;
        }

        string[] headers = lines[0].Split(',');

        Dictionary<string, int> headerMap =
            CreateHeaderMap(headers);

        if (!ValidateHeaders(
                headerMap,
                out string groupHeader))
        {
            return false;
        }

        int successCount = 0;
        int failureCount = 0;

        HashSet<string> registeredEntryKeys =
            new HashSet<string>();

        for (int lineIndex = 1;
             lineIndex < lines.Length;
             lineIndex++)
        {
            string line = lines[lineIndex];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            bool succeeded = TryParseLine(
                line,
                lineIndex + 1,
                headers.Length,
                headerMap,
                groupHeader,
                registeredEntryKeys
            );

            if (succeeded)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }
        }

        SortParsedEntries();
        BuildIndexes();

        IsParsed = true;

        if (showParseLog)
        {
            Debug.Log(
                $"[{nameof(DialogueCsvParser)}] CSV 파싱 완료\n" +
                $"성공: {successCount}개 / 실패: {failureCount}개",
                this
            );
        }

        MarkDirtyInEditor();

        return true;
    }

    /// <summary>
    /// 기본 언어를 기준으로 대사 ID를 검색합니다.
    /// </summary>
    public DialogueEntryData GetEntryById(string id)
    {
        return GetEntryById(
            id,
            defaultLanguage
        );
    }

    /// <summary>
    /// 지정한 언어와 대사 ID를 검색합니다.
    /// </summary>
    public DialogueEntryData GetEntryById(
        string id,
        string language)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        string key = CreateEntryKey(
            id,
            language
        );

        entryById.TryGetValue(
            key,
            out DialogueEntryData entry
        );

        return entry;
    }

    /// <summary>
    /// 기본 언어를 기준으로 그룹의 대사를 반환합니다.
    /// </summary>
    public IReadOnlyList<DialogueEntryData> GetEntriesByGroup(
        string groupId)
    {
        return GetEntriesByGroup(
            groupId,
            defaultLanguage
        );
    }

    /// <summary>
    /// 지정한 언어와 그룹의 대사를 Sequence 순서로 반환합니다.
    /// </summary>
    public IReadOnlyList<DialogueEntryData> GetEntriesByGroup(
        string groupId,
        string language)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Array.Empty<DialogueEntryData>();
        }

        string key = CreateGroupKey(
            groupId,
            language
        );

        if (entriesByGroup.TryGetValue(
                key,
                out List<DialogueEntryData> entries))
        {
            return entries;
        }

        return Array.Empty<DialogueEntryData>();
    }

    public bool HasGroup(string groupId)
    {
        return HasGroup(
            groupId,
            defaultLanguage
        );
    }

    public bool HasGroup(
        string groupId,
        string language)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return false;
        }

        string key = CreateGroupKey(
            groupId,
            language
        );

        return entriesByGroup.ContainsKey(key);
    }

    private bool TryParseLine(
        string line,
        int csvLineNumber,
        int expectedColumnCount,
        Dictionary<string, int> headerMap,
        string groupHeader,
        HashSet<string> registeredEntryKeys)
    {
        /*
         * text 내부의 콤마는 *로 대체되어 있으므로
         * 일반적인 쉼표 분리를 사용합니다.
         */
        string[] columns = line.Split(',');

        if (columns.Length != expectedColumnCount)
        {
            Debug.LogWarning(
                $"[{nameof(DialogueCsvParser)}] " +
                $"{csvLineNumber}행의 열 개수가 올바르지 않습니다. " +
                $"예상: {expectedColumnCount}, " +
                $"실제: {columns.Length}",
                this
            );

            return false;
        }

        string id = GetField(
            columns,
            headerMap,
            "id"
        );

        string language = GetField(
            columns,
            headerMap,
            "language"
        );

        string groupId = GetField(
            columns,
            headerMap,
            groupHeader
        );

        string sequenceValue = GetField(
            columns,
            headerMap,
            "sequence"
        );

        string speaker = GetField(
            columns,
            headerMap,
            "speaker"
        );

        string text = RestoreText(
            GetField(
                columns,
                headerMap,
                "text"
            )
        );

        if (string.IsNullOrWhiteSpace(id))
        {
            LogInvalidField(
                csvLineNumber,
                "id"
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            LogInvalidField(
                csvLineNumber,
                "language"
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            LogInvalidField(
                csvLineNumber,
                groupHeader
            );

            return false;
        }

        if (!int.TryParse(
                sequenceValue,
                out int sequence))
        {
            Debug.LogWarning(
                $"[{nameof(DialogueCsvParser)}] " +
                $"{csvLineNumber}행의 sequence가 올바르지 않습니다: " +
                sequenceValue,
                this
            );

            return false;
        }

        if (sequence < 1)
        {
            Debug.LogWarning(
                $"[{nameof(DialogueCsvParser)}] " +
                $"{csvLineNumber}행의 sequence는 1 이상이어야 합니다.",
                this
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            LogInvalidField(
                csvLineNumber,
                "text"
            );

            return false;
        }

        string entryKey = CreateEntryKey(
            id,
            language
        );

        if (!registeredEntryKeys.Add(entryKey))
        {
            Debug.LogWarning(
                $"[{nameof(DialogueCsvParser)}] " +
                $"{csvLineNumber}행에 중복 ID가 있습니다: " +
                $"{id} / {language}",
                this
            );

            return false;
        }

        DialogueEntryData entry =
            new DialogueEntryData(
                id,
                language,
                groupId,
                sequence,
                speaker,
                text
            );

        parsedEntries.Add(entry);

        return true;
    }

    /// <summary>
    /// Inspector에 저장된 Parsed Entries를 바탕으로
    /// 런타임 검색용 Dictionary를 구성합니다.
    /// </summary>
    private void BuildIndexes()
    {
        entryById.Clear();
        entriesByGroup.Clear();

        for (int i = 0;
             i < parsedEntries.Count;
             i++)
        {
            DialogueEntryData entry =
                parsedEntries[i];

            if (entry == null)
            {
                continue;
            }

            string entryKey = CreateEntryKey(
                entry.Id,
                entry.Language
            );

            if (!entryById.ContainsKey(entryKey))
            {
                entryById.Add(
                    entryKey,
                    entry
                );
            }

            string groupKey = CreateGroupKey(
                entry.GroupId,
                entry.Language
            );

            if (!entriesByGroup.TryGetValue(
                    groupKey,
                    out List<DialogueEntryData> groupEntries))
            {
                groupEntries =
                    new List<DialogueEntryData>();

                entriesByGroup.Add(
                    groupKey,
                    groupEntries
                );
            }

            groupEntries.Add(entry);
        }

        foreach (
            KeyValuePair<string, List<DialogueEntryData>> pair
            in entriesByGroup)
        {
            pair.Value.Sort(
                (left, right) =>
                    left.Sequence.CompareTo(right.Sequence)
            );
        }

        IsParsed = parsedEntries.Count > 0;
    }

    private static Dictionary<string, int> CreateHeaderMap(
        string[] headers)
    {
        Dictionary<string, int> headerMap =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase
            );

        for (int i = 0;
             i < headers.Length;
             i++)
        {
            string header = CleanField(
                headers[i]
            );

            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            if (!headerMap.ContainsKey(header))
            {
                headerMap.Add(
                    header,
                    i
                );
            }
        }

        return headerMap;
    }

    private bool ValidateHeaders(
        Dictionary<string, int> headerMap,
        out string groupHeader)
    {
        groupHeader =
            FindGroupHeader(headerMap);

        string[] requiredHeaders =
        {
            "id",
            "language",
            "sequence",
            "speaker",
            "text"
        };

        for (int i = 0;
             i < requiredHeaders.Length;
             i++)
        {
            string requiredHeader =
                requiredHeaders[i];

            if (headerMap.ContainsKey(requiredHeader))
            {
                continue;
            }

            Debug.LogError(
                $"[{nameof(DialogueCsvParser)}] " +
                $"필수 헤더가 없습니다: {requiredHeader}",
                this
            );

            return false;
        }

        if (string.IsNullOrEmpty(groupHeader))
        {
            Debug.LogError(
                $"[{nameof(DialogueCsvParser)}] " +
                "그룹 식별 헤더가 없습니다. " +
                "group_id, cutscene_id, scene_id 중 하나가 필요합니다.",
                this
            );

            return false;
        }

        return true;
    }

    private static string FindGroupHeader(
        Dictionary<string, int> headerMap)
    {
        for (int i = 0;
             i < GroupHeaderAliases.Length;
             i++)
        {
            string alias =
                GroupHeaderAliases[i];

            if (headerMap.ContainsKey(alias))
            {
                return alias;
            }
        }

        return string.Empty;
    }

    private static string GetField(
        string[] columns,
        Dictionary<string, int> headerMap,
        string header)
    {
        if (!headerMap.TryGetValue(
                header,
                out int index))
        {
            return string.Empty;
        }

        if (index < 0 ||
            index >= columns.Length)
        {
            return string.Empty;
        }

        return CleanField(
            columns[index]
        );
    }

    private static string RestoreText(
        string value)
    {
        return CleanField(value)
            // CSV에서 콤마 대신 사용한 * 복원
            .Replace("*", ",")

            // 문자열 \n을 실제 줄바꿈으로 복원
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n");
    }

    private static string CleanField(
        string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string result = value
            .Trim()
            .TrimStart('\uFEFF');

        if (result.Length >= 2 &&
            result[0] == '"' &&
            result[result.Length - 1] == '"')
        {
            result = result.Substring(
                1,
                result.Length - 2
            );

            result = result.Replace(
                "\"\"",
                "\""
            );
        }

        return result;
    }

    private void SortParsedEntries()
    {
        parsedEntries.Sort(
            (left, right) =>
            {
                int languageCompare =
                    string.Compare(
                        left.Language,
                        right.Language,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (languageCompare != 0)
                {
                    return languageCompare;
                }

                int groupCompare =
                    string.Compare(
                        left.GroupId,
                        right.GroupId,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (groupCompare != 0)
                {
                    return groupCompare;
                }

                return left.Sequence.CompareTo(
                    right.Sequence
                );
            }
        );
    }

    private void ClearDataInternal()
    {
        parsedEntries.Clear();
        entryById.Clear();
        entriesByGroup.Clear();

        IsParsed = false;
    }

    private void LogInvalidField(
        int csvLineNumber,
        string fieldName)
    {
        Debug.LogWarning(
            $"[{nameof(DialogueCsvParser)}] " +
            $"{csvLineNumber}행의 {fieldName} 값이 비어 있습니다.",
            this
        );
    }

    private static string CreateEntryKey(
        string id,
        string language)
    {
        return
            $"{Normalize(language)}|{Normalize(id)}";
    }

    private static string CreateGroupKey(
        string groupId,
        string language)
    {
        return
            $"{Normalize(language)}|{Normalize(groupId)}";
    }

    private static string Normalize(
        string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private void MarkDirtyInEditor()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

#if UNITY_EDITOR

    [ContextMenu("Parse CSV")]
    private void ParseCsvFromContextMenu()
    {
        Parse();
    }

    [ContextMenu("Clear Parsed Entries")]
    private void ClearParsedEntriesFromContextMenu()
    {
        ClearDataInternal();
        MarkDirtyInEditor();
    }

    [ContextMenu("Log Parsed Entries")]
    private void LogParsedEntries()
    {
        for (int i = 0;
             i < parsedEntries.Count;
             i++)
        {
            DialogueEntryData entry =
                parsedEntries[i];

            Debug.Log(
                $"[{entry.Language} / {entry.GroupId}] " +
                $"{entry.Sequence} / {entry.Id} / {entry.Speaker}\n" +
                entry.Text,
                this
            );
        }
    }

#endif
}