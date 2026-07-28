using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [Header("Red Mental Icon & Gauge")]
    [SerializeField] private Image redMentalIcon;
    [SerializeField] private Sprite highMentalIcon;
    [SerializeField] private Sprite mediumMentalIcon;
    [SerializeField] private Sprite lowMentalIcon;
    [SerializeField] private Image mentalGauge;

    [Header("Reload Gauge")]
    [SerializeField] private Image reloadGauge;

    [Tooltip("재장전 중이 아닐 때 리로드 게이지를 숨깁니다.")]
    [SerializeField]
    private bool hideReloadGaugeWhenIdle = true;

    [Header("Experience & Level")]
    [Tooltip("Image Type이 Filled로 설정된 경험치 게이지")]
    [SerializeField]
    private Image experienceGauge;

    [Tooltip("현재 레벨을 표시하는 TMP 텍스트")]
    [SerializeField]
    private TMP_Text levelText;

    [SerializeField, Min(0.0001f)]
    private float fullBarDuration = 0.35f;

    [SerializeField, Min(0.0001f)]
    private float minimumSegmentDuration = 0.08f;

    [SerializeField, Min(0.0001f)]
    private float fullHoldDuration = 0.10f;

    [Header("Game Timer")]
    [Tooltip("플레이 시간을 표시하는 TMP 텍스트")]
    [SerializeField]
    private TMP_Text timerText;

    [Tooltip("새로운 스테이지가 시작될 때 시간을 초기화합니다.")]
    [SerializeField]
    private bool resetTimerOnStageStart;

    [Header("Mental Text References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform mentalTextLayer;
    [SerializeField] private MentalTextPool mentalTextPool;

    [Header("Player Search")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Camera worldCamera;

    [Header("Mental Text Position")]
    [Tooltip("플레이어와 텍스트 사이의 최소 거리")]
    [SerializeField, Min(0f)]
    private float minTextDistance = 130f;

    [Tooltip("플레이어와 텍스트 사이의 최대 거리")]
    [SerializeField, Min(0f)]
    private float maxTextDistance = 380f;

    [Tooltip("텍스트가 화면 밖으로 나가지 않도록 하는 여백")]
    [SerializeField, Min(0f)]
    private float textScreenMargin = 120f;

    [Header("Mental Text Interval")]
    [Tooltip("Mental이 비교적 높을 때 텍스트 출력 간격")]
    [SerializeField]
    private Vector2 slowTextInterval =
        new Vector2(5f, 8f);

    [Tooltip("Mental이 매우 낮을 때 텍스트 출력 간격")]
    [SerializeField]
    private Vector2 fastTextInterval =
        new Vector2(1f, 2.5f);

    [Header("Temporary Mental Text List")]
    [SerializeField]
    private List<string> mediumMentalTexts =
        new List<string>
        {
            "숲이... 원래 이렇게 어두웠던가?",
            "누군가 따라오고 있어.",
            "뒤를 돌아보지 마."
        };

    [SerializeField]
    private List<string> lowMentalTexts =
        new List<string>
        {
            "저건 동물이 아니야.",
            "할머니의 목소리를 믿으면 안 돼.",
            "전부 거짓말이야.",
            "도망쳐."
        };

    [Header("Ammo")]
    [SerializeField] private TMP_Text ammoText;

    private Transform playerTransform;

    private GameManager gameManager;
    private PlayerExperience playerExperience;
    private ExperienceProgressPresentation experiencePresentation;

    private bool isProgressUIBound;
    private bool hasWarnedMissingPlayerExperience;
    private bool isExperiencePresentationActive;

    private int authoritativeExperience;
    private int authoritativeRequiredExperience = 1;
    private int authoritativeLevel = 1;
    private int expectedExperience;
    private int expectedRequiredExperience = 1;
    private int expectedLevel = 1;

    private float currentMental = 100f;
    private float maxMental = 100f;

    private EMentalState currentMentalState =
        EMentalState.High;

    private float mentalTextTimer;

    private float elapsedPlayTime;
    private int displayedTimerSecond = -1;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        InitializeReferences();
        InitializeMentalUI();
        InitializeProgressUI();
    }

    private void Start()
    {
        TryFindPlayer();
        TryBindProgressUI();
        SynchronizeAmmoUI();
    }

    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        if (!isProgressUIBound)
        {
            TryBindProgressUI();
        }

        AdvanceExperiencePresentation(
            Time.unscaledDeltaTime
        );
        UpdateMentalTextSpawner();
        UpdateGameTimer();
    }

    private void OnDisable()
    {
        UnbindProgressUI();
    }

    private void InitializeReferences()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (mentalTextPool == null &&
            mentalTextLayer != null)
        {
            mentalTextPool =
                mentalTextLayer
                    .GetComponentInChildren<MentalTextPool>(
                        true
                    );
        }
    }

    private void InitializeMentalUI()
    {
        currentMental = maxMental;
        currentMentalState = EMentalState.High;

        if (mentalGauge != null)
        {
            mentalGauge.fillAmount = 1f;
        }

        if (reloadGauge != null)
        {
            reloadGauge.fillAmount = 0f;

            if (hideReloadGaugeWhenIdle)
            {
                reloadGauge.gameObject.SetActive(
                    false
                );
            }
        }

        UpdateMentalIcon();
        ResetMentalTextTimer();
    }

    private void InitializeProgressUI()
    {
        EnsureExperiencePresentation();
        experiencePresentation.Reset(
            0,
            PlayerExperience.ExperienceRequiredPerLevel,
            1
        );
        ApplyExperiencePresentation();
        ResetGameTimer();
    }

    private void SynchronizeAmmoUI()
    {
        PlayerAmmo playerAmmo =
            FindFirstObjectByType<PlayerAmmo>();

        if (playerAmmo == null)
        {
            return;
        }

        UpdateAmmo(
            playerAmmo.CurrentAmmo,
            playerAmmo.MaxAmmo
        );

        if (playerAmmo.IsReloading)
        {
            StartReloadGauge();
            UpdateReloadGauge(
                playerAmmo.ReloadProgress
            );
            return;
        }

        EndReloadGauge();
    }

    private void TryBindProgressUI()
    {
        if (isProgressUIBound ||
            !GameManager.HasInstance)
        {
            return;
        }

        GameManager foundGameManager =
            GameManager.Instance;

        PlayerExperience foundPlayerExperience =
            foundGameManager
                .GetComponent<PlayerExperience>();

        if (foundPlayerExperience == null)
        {
            if (!hasWarnedMissingPlayerExperience)
            {
                Debug.LogWarning(
                    $"{nameof(UIManager)}: " +
                    "GameManager 오브젝트에서 " +
                    "PlayerExperience를 찾을 수 없습니다.",
                    this
                );

                hasWarnedMissingPlayerExperience =
                    true;
            }

            return;
        }

        gameManager = foundGameManager;
        playerExperience =
            foundPlayerExperience;

        gameManager.OnPlayerLevelChanged +=
            HandlePlayerLevelChanged;

        gameManager.OnStageStarted +=
            HandleStageStarted;

        playerExperience.OnExperienceChanged +=
            HandleExperienceChanged;

        playerExperience.OnExperienceAdded +=
            HandleExperienceAdded;

        playerExperience.OnLevelGained +=
            HandleLevelGained;

        isProgressUIBound = true;
        hasWarnedMissingPlayerExperience = false;

        SynchronizeExperiencePresentation();
    }

    private void UnbindProgressUI()
    {
        if (!isProgressUIBound)
        {
            return;
        }

        SynchronizeExperiencePresentation();

        if (gameManager != null)
        {
            gameManager.OnPlayerLevelChanged -=
                HandlePlayerLevelChanged;

            gameManager.OnStageStarted -=
                HandleStageStarted;
        }

        if (playerExperience != null)
        {
            playerExperience.OnExperienceChanged -=
                HandleExperienceChanged;

            playerExperience.OnExperienceAdded -=
                HandleExperienceAdded;

            playerExperience.OnLevelGained -=
                HandleLevelGained;
        }

        gameManager = null;
        playerExperience = null;
        isProgressUIBound = false;
    }

    private void HandleExperienceChanged(
        int currentExperience,
        int requiredExperience
    )
    {
        CaptureAuthoritativeSnapshot(
            currentExperience,
            requiredExperience,
            gameManager != null
                ? gameManager.CurrentPlayerLevel
                : authoritativeLevel
        );

        if (!isExperiencePresentationActive)
        {
            SynchronizeExperiencePresentation();
        }
    }

    private void HandlePlayerLevelChanged(
        int newLevel
    )
    {
        authoritativeLevel = Mathf.Max(1, newLevel);

        if (!isExperiencePresentationActive)
        {
            SynchronizeExperiencePresentation();
        }
    }

    private void HandleExperienceAdded(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureExperiencePresentation();

        if (!isExperiencePresentationActive)
        {
            expectedExperience =
                authoritativeExperience;
            expectedRequiredExperience =
                authoritativeRequiredExperience;
            expectedLevel = authoritativeLevel;
            isExperiencePresentationActive = true;
        }

        long combinedExperience =
            (long)expectedExperience + amount;
        int requiredExperience = Mathf.Max(
            1,
            expectedRequiredExperience
        );
        long gainedLevels =
            combinedExperience / requiredExperience;

        expectedExperience = (int)(
            combinedExperience % requiredExperience
        );
        expectedLevel = gainedLevels >=
                        int.MaxValue - expectedLevel
            ? int.MaxValue
            : expectedLevel + (int)gainedLevels;

        experiencePresentation.EnqueueExperience(
            amount
        );
    }

    private void HandleLevelGained(int newLevel)
    {
        if (!isExperiencePresentationActive)
        {
            SynchronizeExperiencePresentation();
            return;
        }

        experiencePresentation.EnqueueLevel(newLevel);
    }

    private void HandleStageStarted(
        int stageIndex
    )
    {
        SynchronizeExperiencePresentation();

        /*
         * 첫 번째 스테이지는 새 게임 시작으로 간주해
         * 항상 타이머를 초기화합니다.
         */
        if (stageIndex == 0 ||
            resetTimerOnStageStart)
        {
            ResetGameTimer();
        }
    }

    private void AdvanceExperiencePresentation(
        float deltaTime
    )
    {
        if (experiencePresentation == null)
        {
            return;
        }

        if (isExperiencePresentationActive &&
            !AuthoritativeSnapshotMatchesExpected())
        {
            SynchronizeExperiencePresentation();
            return;
        }

        experiencePresentation.Advance(
            Mathf.Max(0f, deltaTime)
        );
        ApplyExperiencePresentation();

        if (!isExperiencePresentationActive ||
            experiencePresentation.IsAnimating)
        {
            return;
        }

        SynchronizeExperiencePresentation();
    }

    private void SynchronizeExperiencePresentation()
    {
        EnsureExperiencePresentation();

        if (playerExperience != null &&
            gameManager != null)
        {
            CaptureAuthoritativeSnapshot(
                playerExperience.CurrentExperience,
                playerExperience.RequiredExperience,
                gameManager.CurrentPlayerLevel
            );
        }

        expectedExperience =
            authoritativeExperience;
        expectedRequiredExperience =
            authoritativeRequiredExperience;
        expectedLevel = authoritativeLevel;

        experiencePresentation.Configure(
            fullBarDuration,
            minimumSegmentDuration,
            fullHoldDuration
        );
        experiencePresentation.Reset(
            authoritativeExperience,
            authoritativeRequiredExperience,
            authoritativeLevel
        );
        isExperiencePresentationActive = false;
        ApplyExperiencePresentation();
    }

    private void CaptureAuthoritativeSnapshot(
        int currentExperience,
        int requiredExperience,
        int level
    )
    {
        authoritativeRequiredExperience =
            Mathf.Max(1, requiredExperience);
        authoritativeExperience = Mathf.Clamp(
            currentExperience,
            0,
            authoritativeRequiredExperience - 1
        );
        authoritativeLevel = Mathf.Max(1, level);
    }

    private bool AuthoritativeSnapshotMatchesExpected()
    {
        return authoritativeExperience ==
               expectedExperience &&
               authoritativeRequiredExperience ==
               expectedRequiredExperience &&
               authoritativeLevel == expectedLevel;
    }

    private void EnsureExperiencePresentation()
    {
        if (experiencePresentation != null)
        {
            return;
        }

        experiencePresentation =
            new ExperienceProgressPresentation(
                fullBarDuration,
                minimumSegmentDuration,
                fullHoldDuration
            );
    }

    private void ApplyExperiencePresentation()
    {
        if (experienceGauge != null)
        {
            experienceGauge.fillAmount =
                Mathf.Clamp01(
                    experiencePresentation.FillAmount
                );
        }

        UpdateLevel(
            experiencePresentation.DisplayedLevel
        );
    }

    public void UpdateExperience(
        int currentExperience,
        int requiredExperience
    )
    {
        if (experienceGauge == null)
        {
            return;
        }

        requiredExperience = Mathf.Max(
            1,
            requiredExperience
        );

        currentExperience = Mathf.Clamp(
            currentExperience,
            0,
            requiredExperience
        );

        experienceGauge.fillAmount =
            (float)currentExperience /
            requiredExperience;
    }

    public void UpdateLevel(int level)
    {
        if (levelText == null)
        {
            return;
        }

        level = Mathf.Max(1, level);

        levelText.text =
            $"Lv. {level}";
    }

    private void UpdateGameTimer()
    {
        if (!GameManager.HasInstance ||
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        elapsedPlayTime += Time.deltaTime;

        int currentSecond =
            Mathf.FloorToInt(
                elapsedPlayTime
            );

        if (currentSecond ==
            displayedTimerSecond)
        {
            return;
        }

        displayedTimerSecond =
            currentSecond;

        UpdateTimerText(currentSecond);
    }

    private void UpdateTimerText(
        int totalSeconds
    )
    {
        if (timerText == null)
        {
            return;
        }

        totalSeconds = Mathf.Max(
            0,
            totalSeconds
        );

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        timerText.text =
            $"{minutes:00} : {seconds:00}";
    }

    public void ResetGameTimer()
    {
        elapsedPlayTime = 0f;
        displayedTimerSecond = 0;

        UpdateTimerText(0);
    }

    /// <summary>
    /// Mental 수치가 변경되었을 때 호출합니다.
    /// </summary>
    public void UpdateMental(
        float newCurrentMental,
        float newMaxMental,
        EMentalState newMentalState
    )
    {
        maxMental = Mathf.Max(
            1f,
            newMaxMental
        );

        currentMental = Mathf.Clamp(
            newCurrentMental,
            0f,
            maxMental
        );

        float mentalRatio =
            currentMental / maxMental;

        if (mentalGauge != null)
        {
            mentalGauge.fillAmount =
                mentalRatio;
        }

        if (currentMentalState !=
            newMentalState)
        {
            currentMentalState =
                newMentalState;

            UpdateMentalIcon();
            ResetMentalTextTimer();
        }
    }

    /// <summary>
    /// 현재 탄환 수가 변경되었을 때 호출합니다.
    /// </summary>
    public void UpdateAmmo(
        int currentAmmo,
        int maxAmmo
    )
    {
        if (ammoText == null)
        {
            return;
        }

        maxAmmo = Mathf.Max(
            0,
            maxAmmo
        );

        currentAmmo = Mathf.Clamp(
            currentAmmo,
            0,
            maxAmmo
        );

        ammoText.text =
            $"{currentAmmo} / {maxAmmo}";
    }

    private void UpdateMentalIcon()
    {
        if (redMentalIcon == null)
        {
            return;
        }

        switch (currentMentalState)
        {
            case EMentalState.High:
                redMentalIcon.sprite =
                    highMentalIcon;
                break;

            case EMentalState.Medium:
                redMentalIcon.sprite =
                    mediumMentalIcon;
                break;

            case EMentalState.Low:
                redMentalIcon.sprite =
                    lowMentalIcon;
                break;
        }
    }

    private void UpdateMentalTextSpawner()
    {
        if (!GameManager.HasInstance ||
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        if (currentMentalState ==
            EMentalState.High)
        {
            return;
        }

        mentalTextTimer -= Time.deltaTime;

        if (mentalTextTimer > 0f)
        {
            return;
        }

        TryShowMentalText();
        ResetMentalTextTimer();
    }

    private void TryShowMentalText()
    {
        if (mentalTextPool == null)
        {
            return;
        }

        if (!mentalTextPool.TryGet(
                out MentalText availableText))
        {
            return;
        }

        List<string> textList =
            currentMentalState ==
            EMentalState.Low
                ? lowMentalTexts
                : mediumMentalTexts;

        if (textList == null ||
            textList.Count == 0)
        {
            return;
        }

        if (!TryGetRandomTextPosition(
                out Vector2 textPosition))
        {
            return;
        }

        string message =
            textList[
                Random.Range(
                    0,
                    textList.Count
                )
            ];

        RectTransform textRectTransform =
            availableText.transform
                as RectTransform;

        if (textRectTransform != null)
        {
            textRectTransform.anchoredPosition =
                textPosition;
        }

        availableText.Show(message);
    }

    private bool TryGetRandomTextPosition(
        out Vector2 resultPosition
    )
    {
        resultPosition = Vector2.zero;

        if (mentalTextLayer == null)
        {
            return false;
        }

        if (!TryFindPlayer())
        {
            return false;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            return false;
        }

        Vector3 screenPosition =
            worldCamera.WorldToScreenPoint(
                playerTransform.position
            );

        if (screenPosition.z < 0f)
        {
            return false;
        }

        Camera canvasCamera = null;

        if (canvas != null &&
            canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera =
                canvas.worldCamera;
        }

        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    mentalTextLayer,
                    screenPosition,
                    canvasCamera,
                    out Vector2 playerLocalPosition
                );

        if (!converted)
        {
            return false;
        }

        Vector2 direction =
            Random.insideUnitCircle;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.right;
        }

        direction.Normalize();

        float minimumDistance = Mathf.Min(
            minTextDistance,
            maxTextDistance
        );

        float maximumDistance = Mathf.Max(
            minTextDistance,
            maxTextDistance
        );

        float distance = Random.Range(
            minimumDistance,
            maximumDistance
        );

        resultPosition =
            playerLocalPosition +
            direction * distance;

        ClampTextPositionToLayer(
            ref resultPosition
        );

        return true;
    }

    private void ClampTextPositionToLayer(
        ref Vector2 position
    )
    {
        Rect layerRect =
            mentalTextLayer.rect;

        float minimumX =
            layerRect.xMin +
            textScreenMargin;

        float maximumX =
            layerRect.xMax -
            textScreenMargin;

        float minimumY =
            layerRect.yMin +
            textScreenMargin;

        float maximumY =
            layerRect.yMax -
            textScreenMargin;

        position.x = Mathf.Clamp(
            position.x,
            minimumX,
            maximumX
        );

        position.y = Mathf.Clamp(
            position.y,
            minimumY,
            maximumY
        );
    }

    private bool TryFindPlayer()
    {
        if (playerTransform != null)
        {
            return true;
        }

        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                playerTag
            );

        if (playerObject == null)
        {
            return false;
        }

        playerTransform =
            playerObject.transform;

        return true;
    }

    private void ResetMentalTextTimer()
    {
        float mentalPercent =
            currentMental /
            maxMental * 100f;

        float dangerRatio =
            Mathf.InverseLerp(
                66f,
                0f,
                mentalPercent
            );

        float minimumInterval =
            Mathf.Lerp(
                slowTextInterval.x,
                fastTextInterval.x,
                dangerRatio
            );

        float maximumInterval =
            Mathf.Lerp(
                slowTextInterval.y,
                fastTextInterval.y,
                dangerRatio
            );

        if (minimumInterval >
            maximumInterval)
        {
            (
                minimumInterval,
                maximumInterval
            ) = (
                maximumInterval,
                minimumInterval
            );
        }

        mentalTextTimer =
            Random.Range(
                minimumInterval,
                maximumInterval
            );
    }

    /// <summary>
    /// 재장전을 시작할 때 호출합니다.
    /// </summary>
    public void StartReloadGauge()
    {
        if (reloadGauge == null)
        {
            return;
        }

        reloadGauge.fillAmount = 0f;
        reloadGauge.gameObject.SetActive(
            true
        );
    }

    /// <summary>
    /// 재장전 진행률을 갱신합니다.
    /// normalizedProgress는 0~1 범위입니다.
    /// </summary>
    public void UpdateReloadGauge(
        float normalizedProgress
    )
    {
        if (reloadGauge == null)
        {
            return;
        }

        reloadGauge.fillAmount =
            Mathf.Clamp01(
                normalizedProgress
            );
    }

    /// <summary>
    /// 재장전이 완료되거나 취소되었을 때 호출합니다.
    /// </summary>
    public void EndReloadGauge()
    {
        if (reloadGauge == null)
        {
            return;
        }

        reloadGauge.fillAmount = 0f;

        if (hideReloadGaugeWhenIdle)
        {
            reloadGauge.gameObject.SetActive(
                false
            );
        }
    }
}
