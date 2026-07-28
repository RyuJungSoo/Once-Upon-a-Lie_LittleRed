using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum EEpilogueState
{
    None,
    OpeningDelay,
    CutsceneFadeIn,
    Typing,
    WaitingForInput,
    WaitingForEndingInput,
    CutsceneFadeOut,
    EndingTextFadeIn,
    Completed
}

public sealed class EpilogueController : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private DialogueCsvParser dialogueCsvParser;

    [Header("UI")]
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform endingTextsRoot;
    [SerializeField] private Button theEndButton;

    [Header("Cutscene Assets")]
    [SerializeField]
    private List<CutsceneAssetData> cutsceneAssets =
        new List<CutsceneAssetData>();

    [Header("Opening")]
    [SerializeField, Min(0f)]
    private float openingDelay = 0.25f;

    [Header("Cutscene Fade")]
    [SerializeField, Min(0.01f)]
    private float cutsceneFadeInDuration = 0.8f;

    [SerializeField, Min(0.01f)]
    private float cutsceneFadeOutDuration = 0.8f;

    [Header("Typewriter")]
    [SerializeField, Min(0.001f)]
    private float characterInterval = 0.035f;

    [Header("Input")]
    [SerializeField, Min(0f)]
    private float inputCooldown = 0.15f;

    [Header("Ending Text")]
    [SerializeField, Min(0.01f)]
    private float endingTextFadeDuration = 0.8f;

    [SerializeField, Min(0f)]
    private float endingTextInterval = 0.5f;

    [SerializeField, Min(0f)]
    private float theEndButtonDelay = 1.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onEpilogueCompleted;
    [SerializeField] private UnityEvent onTheEndSelected;

    [Header("Runtime Debug")]
    [SerializeField]
    private EEpilogueState currentState =
        EEpilogueState.None;

    [SerializeField] private int currentEntryIndex;
    [SerializeField] private string currentGroupId;

    private readonly Dictionary<string, Sprite>
        cutsceneSpriteByGroupId =
            new Dictionary<string, Sprite>();

    private readonly List<DialogueEntryData>
        playbackEntries =
            new List<DialogueEntryData>();

    private readonly List<CanvasGroup>
        endingTextCanvasGroups =
            new List<CanvasGroup>();

    private Coroutine openingCoroutine;
    private Coroutine cutsceneFadeCoroutine;
    private Coroutine typewriterCoroutine;
    private Coroutine endingSequenceCoroutine;

    private string currentFullText = string.Empty;
    private float nextInputAllowedTime;

    public EEpilogueState CurrentState => currentState;

    public bool IsCompleted =>
        currentState == EEpilogueState.Completed;

    private void Awake()
    {
        BuildCutsceneAssetIndex();
        BuildEndingTextList();

        if (theEndButton != null)
        {
            theEndButton.onClick.AddListener(
                SelectTheEnd
            );
        }
    }

    private void OnEnable()
    {
        StartEpilogue();
    }

    private void OnDisable()
    {
        StopRunningCoroutines();

        currentState =
            EEpilogueState.None;
    }

    private void OnDestroy()
    {
        if (theEndButton != null)
        {
            theEndButton.onClick.RemoveListener(
                SelectTheEnd
            );
        }
    }

    private void Update()
    {
        bool canReceiveAdvanceInput =
            currentState == EEpilogueState.Typing ||
            currentState == EEpilogueState.WaitingForInput ||
            currentState == EEpilogueState.WaitingForEndingInput;

        if (!canReceiveAdvanceInput)
        {
            return;
        }

        if (Time.unscaledTime <
            nextInputAllowedTime)
        {
            return;
        }

        if (!WasAdvanceInputPressedThisFrame())
        {
            return;
        }

        HandleAdvanceInput();
    }

    public void StartEpilogue()
    {
        StopRunningCoroutines();

        BuildCutsceneAssetIndex();
        BuildEndingTextList();

        if (!ValidateReferences())
        {
            return;
        }

        if (!dialogueCsvParser.IsParsed)
        {
            bool parseSucceeded =
                dialogueCsvParser.Parse();

            if (!parseSucceeded)
            {
                Debug.LogError(
                    $"[{nameof(EpilogueController)}] " +
                    "에필로그 대사 CSV 파싱에 실패했습니다.",
                    this
                );

                return;
            }
        }

        BuildPlaybackEntries();
        InitializeUI();

        if (playbackEntries.Count == 0)
        {
            Debug.LogError(
                $"[{nameof(EpilogueController)}] " +
                "출력할 에필로그 대사가 없습니다.",
                this
            );

            return;
        }

        openingCoroutine =
            StartCoroutine(
                BeginEpilogueRoutine()
            );
    }

    private void InitializeUI()
    {
        currentEntryIndex = 0;
        currentGroupId = string.Empty;
        currentFullText = string.Empty;

        currentState =
            EEpilogueState.OpeningDelay;

        nextInputAllowedTime =
            Time.unscaledTime + inputCooldown;

        /*
         * Epilogue_UI가 씬 시작부터 활성화되어 있어도
         * 첫 화면에서 컷신이 보이지 않도록 초기화합니다.
         */
        cutsceneImage.gameObject.SetActive(true);
        cutsceneImage.sprite = null;
        SetGraphicAlpha(cutsceneImage, 0f);

        dialogueText.gameObject.SetActive(true);
        dialogueText.text = string.Empty;
        dialogueText.maxVisibleCharacters = 0;
        SetGraphicAlpha(dialogueText, 1f);

        endingTextsRoot.gameObject.SetActive(true);

        for (int i = 0;
             i < endingTextCanvasGroups.Count;
             i++)
        {
            CanvasGroup canvasGroup =
                endingTextCanvasGroups[i];

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.gameObject.SetActive(false);
        }

        theEndButton.gameObject.SetActive(false);
    }

    private IEnumerator BeginEpilogueRoutine()
    {
        /*
         * Image 알파 0과 엔딩 텍스트 비활성화 상태가
         * 실제 화면에 먼저 반영되도록 한 프레임 기다립니다.
         */
        yield return null;

        if (openingDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                openingDelay
            );
        }

        openingCoroutine = null;

        ShowCurrentEntry();
    }

    private void ShowCurrentEntry()
    {
        if (currentEntryIndex < 0 ||
            currentEntryIndex >= playbackEntries.Count)
        {
            EnterEndingInputWait();
            return;
        }

        DialogueEntryData currentEntry =
            playbackEntries[currentEntryIndex];

        bool isGroupChanged =
            !string.Equals(
                currentGroupId,
                currentEntry.GroupId,
                StringComparison.OrdinalIgnoreCase
            );

        if (isGroupChanged)
        {
            currentGroupId =
                currentEntry.GroupId;

            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = 0;

            bool imageApplied =
                TryApplyCutsceneImage(
                    currentGroupId
                );

            if (imageApplied)
            {
                StartCutsceneFadeIn(
                    currentEntry.Text
                );

                return;
            }
        }

        StartTypewriter(
            currentEntry.Text
        );
    }

    private bool TryApplyCutsceneImage(
        string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return false;
        }

        string normalizedGroupId =
            NormalizeGroupId(groupId);

        bool spriteFound =
            cutsceneSpriteByGroupId.TryGetValue(
                normalizedGroupId,
                out Sprite cutsceneSprite
            );

        if (!spriteFound)
        {
            Debug.LogWarning(
                $"[{nameof(EpilogueController)}] " +
                $"컷신 이미지를 찾지 못했습니다: {groupId}",
                this
            );

            return false;
        }

        cutsceneImage.sprite =
            cutsceneSprite;

        /*
         * AI 컷신 이미지마다 비율과 크기가 다르므로
         * Sprite 변경 시마다 Native Size를 적용합니다.
         */
        cutsceneImage.SetNativeSize();
        cutsceneImage.gameObject.SetActive(true);

        return true;
    }

    private void StartCutsceneFadeIn(
        string dialogue)
    {
        if (cutsceneFadeCoroutine != null)
        {
            StopCoroutine(
                cutsceneFadeCoroutine
            );

            cutsceneFadeCoroutine = null;
        }

        cutsceneFadeCoroutine =
            StartCoroutine(
                FadeInCutsceneThenTypeRoutine(
                    dialogue
                )
            );
    }

    private IEnumerator FadeInCutsceneThenTypeRoutine(
        string dialogue)
    {
        currentState =
            EEpilogueState.CutsceneFadeIn;

        SetGraphicAlpha(
            cutsceneImage,
            0f
        );

        float elapsedTime = 0f;

        while (elapsedTime <
               cutsceneFadeInDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    cutsceneFadeInDuration
                );

            SetGraphicAlpha(
                cutsceneImage,
                progress
            );

            yield return null;
        }

        SetGraphicAlpha(
            cutsceneImage,
            1f
        );

        cutsceneFadeCoroutine = null;

        StartTypewriter(dialogue);
    }

    private void StartTypewriter(
        string text)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(
                typewriterCoroutine
            );

            typewriterCoroutine = null;
        }

        currentFullText =
            text ?? string.Empty;

        dialogueText.gameObject.SetActive(true);
        SetGraphicAlpha(dialogueText, 1f);

        dialogueText.text =
            currentFullText;

        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        currentState =
            EEpilogueState.Typing;

        nextInputAllowedTime =
            Time.unscaledTime + inputCooldown;

        typewriterCoroutine =
            StartCoroutine(
                TypewriterRoutine()
            );
    }

    private IEnumerator TypewriterRoutine()
    {
        dialogueText.ForceMeshUpdate();

        int characterCount =
            dialogueText.textInfo.characterCount;

        int visibleCharacterCount = 0;
        float elapsedTime = 0f;

        while (visibleCharacterCount <
               characterCount)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            while (elapsedTime >= characterInterval &&
                   visibleCharacterCount <
                   characterCount)
            {
                elapsedTime -=
                    characterInterval;

                visibleCharacterCount++;

                dialogueText.maxVisibleCharacters =
                    visibleCharacterCount;
            }

            yield return null;
        }

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        typewriterCoroutine = null;

        FinishCurrentSentence();
    }

    private void HandleAdvanceInput()
    {
        nextInputAllowedTime =
            Time.unscaledTime + inputCooldown;

        switch (currentState)
        {
            case EEpilogueState.Typing:
                CompleteTypewriterImmediately();
                break;

            case EEpilogueState.WaitingForInput:
                MoveToNextEntry();
                break;

            case EEpilogueState.WaitingForEndingInput:
                StartEndingSequence();
                break;
        }
    }

    private void CompleteTypewriterImmediately()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(
                typewriterCoroutine
            );

            typewriterCoroutine = null;
        }

        dialogueText.text =
            currentFullText;

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        FinishCurrentSentence();
    }

    private void FinishCurrentSentence()
    {
        bool isLastEntry =
            currentEntryIndex >=
            playbackEntries.Count - 1;

        if (isLastEntry)
        {
            /*
             * 마지막 문장이 전부 표시되어도
             * 바로 엔딩 텍스트로 넘어가지 않습니다.
             *
             * 추가 입력을 받을 때까지
             * 마지막 컷신과 대사를 그대로 유지합니다.
             */
            EnterEndingInputWait();
            return;
        }

        currentState =
            EEpilogueState.WaitingForInput;

        nextInputAllowedTime =
            Time.unscaledTime + inputCooldown;
    }

    private void MoveToNextEntry()
    {
        currentEntryIndex++;

        if (currentEntryIndex >=
            playbackEntries.Count)
        {
            EnterEndingInputWait();
            return;
        }

        ShowCurrentEntry();
    }

    private void EnterEndingInputWait()
    {
        currentState =
            EEpilogueState.WaitingForEndingInput;

        nextInputAllowedTime =
            Time.unscaledTime + inputCooldown;
    }

    private void StartEndingSequence()
    {
        if (endingSequenceCoroutine != null)
        {
            return;
        }

        if (openingCoroutine != null)
        {
            StopCoroutine(
                openingCoroutine
            );

            openingCoroutine = null;
        }

        if (cutsceneFadeCoroutine != null)
        {
            StopCoroutine(
                cutsceneFadeCoroutine
            );

            cutsceneFadeCoroutine = null;
        }

        if (typewriterCoroutine != null)
        {
            StopCoroutine(
                typewriterCoroutine
            );

            typewriterCoroutine = null;
        }

        endingSequenceCoroutine =
            StartCoroutine(
                EndingSequenceRoutine()
            );
    }

    private IEnumerator EndingSequenceRoutine()
    {
        currentState =
            EEpilogueState.CutsceneFadeOut;

        float imageStartAlpha =
            cutsceneImage.color.a;

        float textStartAlpha =
            dialogueText.color.a;

        float elapsedTime = 0f;

        while (elapsedTime <
               cutsceneFadeOutDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    cutsceneFadeOutDuration
                );

            SetGraphicAlpha(
                cutsceneImage,
                Mathf.Lerp(
                    imageStartAlpha,
                    0f,
                    progress
                )
            );

            SetGraphicAlpha(
                dialogueText,
                Mathf.Lerp(
                    textStartAlpha,
                    0f,
                    progress
                )
            );

            yield return null;
        }

        SetGraphicAlpha(
            cutsceneImage,
            0f
        );

        SetGraphicAlpha(
            dialogueText,
            0f
        );

        cutsceneImage.gameObject.SetActive(false);
        dialogueText.gameObject.SetActive(false);

        currentState =
            EEpilogueState.EndingTextFadeIn;

        for (int i = 0;
             i < endingTextCanvasGroups.Count;
             i++)
        {
            CanvasGroup canvasGroup =
                endingTextCanvasGroups[i];

            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            yield return FadeCanvasGroupRoutine(
                canvasGroup,
                0f,
                1f,
                endingTextFadeDuration
            );

            if (i <
                endingTextCanvasGroups.Count - 1)
            {
                yield return new WaitForSecondsRealtime(
                    endingTextInterval
                );
            }
        }

        if (theEndButtonDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                theEndButtonDelay
            );
        }

        theEndButton.gameObject.SetActive(true);

        currentState =
            EEpilogueState.Completed;

        endingSequenceCoroutine = null;

        onEpilogueCompleted?.Invoke();
    }

    public void SelectTheEnd()
    {
        if (currentState !=
            EEpilogueState.Completed)
        {
            return;
        }

        onTheEndSelected?.Invoke();
    }

    private void BuildPlaybackEntries()
    {
        playbackEntries.Clear();

        HashSet<string> addedGroups =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        /*
         * Cutscene Assets 리스트의 순서를
         * 에필로그 컷신 재생 순서로 사용합니다.
         */
        for (int assetIndex = 0;
             assetIndex < cutsceneAssets.Count;
             assetIndex++)
        {
            CutsceneAssetData assetData =
                cutsceneAssets[assetIndex];

            if (assetData == null ||
                string.IsNullOrWhiteSpace(
                    assetData.GroupId))
            {
                continue;
            }

            if (!addedGroups.Add(
                    assetData.GroupId))
            {
                Debug.LogWarning(
                    $"[{nameof(EpilogueController)}] " +
                    "중복된 재생 그룹입니다: " +
                    assetData.GroupId,
                    this
                );

                continue;
            }

            IReadOnlyList<DialogueEntryData> groupEntries =
                dialogueCsvParser.GetEntriesByGroup(
                    assetData.GroupId
                );

            if (groupEntries.Count == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(EpilogueController)}] " +
                    "그룹에 등록된 대사가 없습니다: " +
                    assetData.GroupId,
                    this
                );

                continue;
            }

            for (int entryIndex = 0;
                 entryIndex < groupEntries.Count;
                 entryIndex++)
            {
                playbackEntries.Add(
                    groupEntries[entryIndex]
                );
            }
        }
    }

    private void BuildCutsceneAssetIndex()
    {
        cutsceneSpriteByGroupId.Clear();

        for (int assetIndex = 0;
             assetIndex < cutsceneAssets.Count;
             assetIndex++)
        {
            CutsceneAssetData assetData =
                cutsceneAssets[assetIndex];

            if (assetData == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    assetData.GroupId))
            {
                Debug.LogWarning(
                    $"[{nameof(EpilogueController)}] " +
                    $"{assetIndex}번 컷신의 " +
                    "Group ID가 비어 있습니다.",
                    this
                );

                continue;
            }

            if (assetData.Sprite == null)
            {
                Debug.LogWarning(
                    $"[{nameof(EpilogueController)}] " +
                    "Sprite가 연결되지 않았습니다: " +
                    assetData.GroupId,
                    this
                );

                continue;
            }

            string normalizedGroupId =
                NormalizeGroupId(
                    assetData.GroupId
                );

            if (cutsceneSpriteByGroupId.ContainsKey(
                    normalizedGroupId))
            {
                Debug.LogWarning(
                    $"[{nameof(EpilogueController)}] " +
                    "중복된 Group ID가 있습니다: " +
                    assetData.GroupId,
                    this
                );

                continue;
            }

            cutsceneSpriteByGroupId.Add(
                normalizedGroupId,
                assetData.Sprite
            );
        }
    }

    private void BuildEndingTextList()
    {
        endingTextCanvasGroups.Clear();

        if (endingTextsRoot == null)
        {
            return;
        }

        for (int childIndex = 0;
             childIndex < endingTextsRoot.childCount;
             childIndex++)
        {
            Transform child =
                endingTextsRoot.GetChild(
                    childIndex
                );

            CanvasGroup canvasGroup =
                child.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                Debug.LogWarning(
                    $"[{nameof(EpilogueController)}] " +
                    "Ending_Texts의 자식에 " +
                    $"CanvasGroup이 없습니다: {child.name}",
                    child
                );

                continue;
            }

            endingTextCanvasGroups.Add(
                canvasGroup
            );
        }
    }

    private static bool
        WasAdvanceInputPressedThisFrame()
    {
        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current.anyKey
                .wasPressedThisFrame;

        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame;

        bool gamepadPressed =
            Gamepad.current != null &&
            (
                Gamepad.current.buttonSouth
                    .wasPressedThisFrame ||
                Gamepad.current.startButton
                    .wasPressedThisFrame
            );

        return keyboardPressed ||
               mousePressed ||
               gamepadPressed;
    }

    private bool ValidateReferences()
    {
        bool hasMissingReference =
            dialogueCsvParser == null ||
            cutsceneImage == null ||
            dialogueText == null ||
            endingTextsRoot == null ||
            theEndButton == null;

        if (hasMissingReference)
        {
            Debug.LogError(
                $"[{nameof(EpilogueController)}] " +
                "Inspector 참조가 모두 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (cutsceneAssets.Count == 0)
        {
            Debug.LogError(
                $"[{nameof(EpilogueController)}] " +
                "Cutscene Assets가 비어 있습니다.",
                this
            );

            return false;
        }

        if (endingTextCanvasGroups.Count == 0)
        {
            Debug.LogError(
                $"[{nameof(EpilogueController)}] " +
                "출력할 Ending Text가 없습니다. " +
                "Ending_Texts의 직계 자식에 " +
                "CanvasGroup을 추가하세요.",
                this
            );

            return false;
        }

        return true;
    }

    private void StopRunningCoroutines()
    {
        if (openingCoroutine != null)
        {
            StopCoroutine(
                openingCoroutine
            );

            openingCoroutine = null;
        }

        if (cutsceneFadeCoroutine != null)
        {
            StopCoroutine(
                cutsceneFadeCoroutine
            );

            cutsceneFadeCoroutine = null;
        }

        if (typewriterCoroutine != null)
        {
            StopCoroutine(
                typewriterCoroutine
            );

            typewriterCoroutine = null;
        }

        if (endingSequenceCoroutine != null)
        {
            StopCoroutine(
                endingSequenceCoroutine
            );

            endingSequenceCoroutine = null;
        }
    }

    private static void SetGraphicAlpha(
        Graphic graphic,
        float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color =
            graphic.color;

        color.a =
            Mathf.Clamp01(alpha);

        graphic.color =
            color;
    }

    private static IEnumerator FadeCanvasGroupRoutine(
        CanvasGroup canvasGroup,
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha =
                targetAlpha;

            yield break;
        }

        float elapsedTime = 0f;

        canvasGroup.alpha =
            startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / duration
                );

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );

            yield return null;
        }

        canvasGroup.alpha =
            targetAlpha;
    }

    private static string NormalizeGroupId(
        string groupId)
    {
        return groupId
            .Trim()
            .ToLowerInvariant();
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            BuildCutsceneAssetIndex();
            BuildEndingTextList();
        }
    }

    [ContextMenu("Start Epilogue")]
    private void StartEpilogueFromContextMenu()
    {
        if (Application.isPlaying)
        {
            StartEpilogue();
        }
    }

#endif
}