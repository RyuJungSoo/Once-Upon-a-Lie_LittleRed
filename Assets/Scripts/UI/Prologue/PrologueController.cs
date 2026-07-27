using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum EPrologueState
{
    None,
    BackgroundFadeIn,
    Typing,
    WaitingForInput,
    Completed
}

public sealed class PrologueController : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private DialogueCsvParser dialogueCsvParser;

    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button nextButton;

    [Header("Cutscene Assets")]
    [SerializeField]
    private List<CutsceneAssetData> cutsceneAssets =
        new List<CutsceneAssetData>();

    [Header("Background Fade")]
    [SerializeField, Min(0.01f)]
    private float backgroundFadeDuration = 1f;

    [Header("Typewriter")]
    [SerializeField, Min(0.001f)]
    private float characterInterval = 0.035f;

    [Header("Input")]
    [SerializeField, Min(0f)]
    private float inputCooldown = 0.15f;

    [Header("Events")]
    [SerializeField] private UnityEvent onPrologueSkipped;
    [SerializeField] private UnityEvent onPrologueCompleted;
    [SerializeField] private UnityEvent onNextSelected;

    [Header("Runtime Debug")]
    [SerializeField]
    private EPrologueState currentState =
        EPrologueState.None;

    [SerializeField] private int currentEntryIndex;
    [SerializeField] private string currentGroupId;

    private readonly Dictionary<string, Sprite>
        cutsceneSpriteByGroupId =
            new Dictionary<string, Sprite>();

    private readonly List<DialogueEntryData>
        playbackEntries =
            new List<DialogueEntryData>();

    private readonly List<RaycastResult>
        uiRaycastResults =
            new List<RaycastResult>();

    private Coroutine backgroundFadeCoroutine;
    private Coroutine typewriterCoroutine;

    private string currentFullText = string.Empty;
    private float nextInputAllowedTime;

    public EPrologueState CurrentState => currentState;

    public bool IsCompleted =>
        currentState == EPrologueState.Completed;

    private void Awake()
    {
        BuildCutsceneAssetIndex();

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(
                SkipPrologue
            );
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(
                SelectNext
            );
        }
    }

    private void OnEnable()
    {
        StartPrologue();
    }

    private void OnDisable()
    {
        StopRunningCoroutines();

        currentState =
            EPrologueState.None;
    }

    private void OnDestroy()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(
                SkipPrologue
            );
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(
                SelectNext
            );
        }
    }

    private void Update()
    {
        if (currentState != EPrologueState.Typing &&
            currentState != EPrologueState.WaitingForInput)
        {
            return;
        }

        if (Time.unscaledTime <
            nextInputAllowedTime)
        {
            return;
        }

        bool keyboardPressed =
            WasKeyboardPressedThisFrame();

        bool mousePressed =
            WasMousePressedThisFrame();

        bool gamepadPressed =
            WasGamepadPressedThisFrame();

        if (!keyboardPressed &&
            !mousePressed &&
            !gamepadPressed)
        {
            return;
        }

        /*
         * 마우스로 Skip 또는 Next 버튼을 누른 경우에는
         * 버튼 이벤트만 실행합니다.
         *
         * 배경, 컷신 이미지, 텍스트 등 다른 UI를 클릭하면
         * 정상적으로 대사가 진행됩니다.
         */
        if (mousePressed &&
            IsPointerOverPrologueButton())
        {
            return;
        }

        HandleAdvanceInput();
    }

    public void StartPrologue()
    {
        StopRunningCoroutines();
        BuildCutsceneAssetIndex();

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
                    $"[{nameof(PrologueController)}] " +
                    "대사 CSV 파싱에 실패했습니다.",
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
                $"[{nameof(PrologueController)}] " +
                "출력할 대사 데이터가 없습니다.",
                this
            );

            return;
        }

        backgroundFadeCoroutine =
            StartCoroutine(
                FadeInBackgroundRoutine()
            );
    }

    private void InitializeUI()
    {
        currentEntryIndex = 0;
        currentGroupId = string.Empty;
        currentFullText = string.Empty;

        currentState =
            EPrologueState.BackgroundFadeIn;

        nextInputAllowedTime =
            Time.unscaledTime + inputCooldown;

        backgroundImage.gameObject.SetActive(true);
        SetBackgroundAlpha(0f);

        cutsceneImage.gameObject.SetActive(false);
        cutsceneImage.sprite = null;

        dialogueText.gameObject.SetActive(true);
        dialogueText.text = string.Empty;
        dialogueText.maxVisibleCharacters = 0;

        skipButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
    }

    private IEnumerator FadeInBackgroundRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime <
               backgroundFadeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float alpha = Mathf.Clamp01(
                elapsedTime /
                backgroundFadeDuration
            );

            SetBackgroundAlpha(alpha);

            yield return null;
        }

        SetBackgroundAlpha(1f);

        backgroundFadeCoroutine = null;

        skipButton.gameObject.SetActive(true);
        cutsceneImage.gameObject.SetActive(true);

        ShowCurrentEntry();
    }

    private void SetBackgroundAlpha(float alpha)
    {
        Color backgroundColor =
            backgroundImage.color;

        backgroundColor.a =
            Mathf.Clamp01(alpha);

        backgroundImage.color =
            backgroundColor;
    }

    private void ShowCurrentEntry()
    {
        if (currentEntryIndex < 0 ||
            currentEntryIndex >=
            playbackEntries.Count)
        {
            CompletePrologue();
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

            ChangeCutsceneImage(
                currentGroupId
            );
        }

        StartTypewriter(
            currentEntry.Text
        );
    }

    private void StartTypewriter(string text)
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

        dialogueText.text =
            currentFullText;

        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        currentState =
            EPrologueState.Typing;

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

        typewriterCoroutine = null;

        FinishCurrentSentence();
    }

    private void HandleAdvanceInput()
    {
        nextInputAllowedTime =
            Time.unscaledTime + inputCooldown;

        switch (currentState)
        {
            case EPrologueState.Typing:
                CompleteTypewriterImmediately();
                break;

            case EPrologueState.WaitingForInput:
                MoveToNextEntry();
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
            CompletePrologue();
            return;
        }

        currentState =
            EPrologueState.WaitingForInput;

        nextInputAllowedTime =
            Time.unscaledTime + inputCooldown;
    }

    private void MoveToNextEntry()
    {
        currentEntryIndex++;

        if (currentEntryIndex >=
            playbackEntries.Count)
        {
            CompletePrologue();
            return;
        }

        ShowCurrentEntry();
    }

    private bool ChangeCutsceneImage(
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
                $"[{nameof(PrologueController)}] " +
                $"컷신 이미지를 찾지 못했습니다: {groupId}",
                this
            );

            return false;
        }

        cutsceneImage.sprite =
            cutsceneSprite;

        /*
         * 컷신 이미지마다 비율과 원본 크기가 다르므로
         * Sprite가 변경될 때마다 Native Size를 적용합니다.
         */
        cutsceneImage.SetNativeSize();

        cutsceneImage.gameObject.SetActive(true);

        return true;
    }

    private void CompletePrologue()
    {
        StopRunningCoroutines();

        currentState =
            EPrologueState.Completed;

        dialogueText.text =
            currentFullText;

        dialogueText.maxVisibleCharacters =
            int.MaxValue;

        skipButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(true);

        onPrologueCompleted?.Invoke();
    }

    public void SkipPrologue()
    {
        if (currentState == EPrologueState.None ||
            currentState == EPrologueState.Completed)
        {
            return;
        }

        StopRunningCoroutines();

        currentState =
            EPrologueState.Completed;

        skipButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);

        onPrologueSkipped?.Invoke();
    }

    public void SelectNext()
    {
        if (currentState !=
            EPrologueState.Completed)
        {
            return;
        }

        onNextSelected?.Invoke();
    }

    private void BuildPlaybackEntries()
    {
        playbackEntries.Clear();

        HashSet<string> addedGroups =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        /*
         * Cutscene Assets에 등록된 순서를
         * 컷신과 대사의 재생 순서로 사용합니다.
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
                continue;
            }

            IReadOnlyList<DialogueEntryData>
                groupEntries =
                    dialogueCsvParser
                        .GetEntriesByGroup(
                            assetData.GroupId
                        );

            if (groupEntries.Count == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(PrologueController)}] " +
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
                    $"[{nameof(PrologueController)}] " +
                    $"{assetIndex}번 컷신의 " +
                    "Group ID가 비어 있습니다.",
                    this
                );

                continue;
            }

            if (assetData.Sprite == null)
            {
                Debug.LogWarning(
                    $"[{nameof(PrologueController)}] " +
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
                    $"[{nameof(PrologueController)}] " +
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

    private static bool
        WasKeyboardPressedThisFrame()
    {
        return Keyboard.current != null &&
               Keyboard.current.anyKey
                   .wasPressedThisFrame;
    }

    private static bool
        WasMousePressedThisFrame()
    {
        return Mouse.current != null &&
               Mouse.current.leftButton
                   .wasPressedThisFrame;
    }

    private static bool
        WasGamepadPressedThisFrame()
    {
        if (Gamepad.current == null)
        {
            return false;
        }

        return
            Gamepad.current.buttonSouth
                .wasPressedThisFrame ||
            Gamepad.current.startButton
                .wasPressedThisFrame;
    }

    private bool IsPointerOverPrologueButton()
    {
        if (EventSystem.current == null ||
            Mouse.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData =
            new PointerEventData(
                EventSystem.current
            )
            {
                position =
                    Mouse.current.position.ReadValue()
            };

        uiRaycastResults.Clear();

        EventSystem.current.RaycastAll(
            pointerEventData,
            uiRaycastResults
        );

        for (int resultIndex = 0;
             resultIndex < uiRaycastResults.Count;
             resultIndex++)
        {
            GameObject hitObject =
                uiRaycastResults[resultIndex]
                    .gameObject;

            Button hitButton =
                hitObject.GetComponentInParent<Button>();

            if (hitButton == null)
            {
                continue;
            }

            bool isSkipButton =
                hitButton == skipButton;

            bool isNextButton =
                hitButton == nextButton;

            if (!isSkipButton &&
                !isNextButton)
            {
                continue;
            }

            if (!hitButton.gameObject.activeInHierarchy ||
                !hitButton.interactable)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool ValidateReferences()
    {
        bool hasMissingReference =
            dialogueCsvParser == null ||
            backgroundImage == null ||
            cutsceneImage == null ||
            dialogueText == null ||
            skipButton == null ||
            nextButton == null;

        if (!hasMissingReference)
        {
            return true;
        }

        Debug.LogError(
            $"[{nameof(PrologueController)}] " +
            "Inspector 참조가 모두 연결되지 않았습니다.",
            this
        );

        return false;
    }

    private void StopRunningCoroutines()
    {
        if (backgroundFadeCoroutine != null)
        {
            StopCoroutine(
                backgroundFadeCoroutine
            );

            backgroundFadeCoroutine = null;
        }

        if (typewriterCoroutine != null)
        {
            StopCoroutine(
                typewriterCoroutine
            );

            typewriterCoroutine = null;
        }
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
        }
    }

    [ContextMenu("Start Prologue")]
    private void StartPrologueFromContextMenu()
    {
        if (Application.isPlaying)
        {
            StartPrologue();
        }
    }

#endif
}