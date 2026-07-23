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
    [Header("Mental Gauge")]
    [SerializeField] private Image mentalGauge;


    [Header("Mental Screen Panels")]
    [SerializeField] private Image topPanel;
    [SerializeField] private Image bottomPanel;
    [SerializeField] private Image leftPanel;
    [SerializeField] private Image rightPanel;

    [Header("Reload Gauge")]
    [SerializeField] private Image reloadGauge;

    [Tooltip("재장전 중이 아닐 때 리로드 게이지를 숨깁니다.")]
    [SerializeField] private bool hideReloadGaugeWhenIdle = true;

    [Header("Mental Screen Size")]
    [Tooltip("Mental이 높을 때 위·아래 패널의 높이")]
    [SerializeField, Min(0f)]
    private float minTopBottomSize = 150f;

    [Tooltip("Mental이 낮을 때 위·아래 패널의 최대 높이")]
    [SerializeField, Min(0f)]
    private float maxTopBottomSize = 320f;

    [Tooltip("Mental이 높을 때 좌·우 패널의 너비")]
    [SerializeField, Min(0f)]
    private float minSideSize = 220f;

    [Tooltip("Mental이 낮을 때 좌·우 패널의 최대 너비")]
    [SerializeField, Min(0f)]
    private float maxSideSize = 470f;

    [Header("Mental Screen Darkness")]
    [Tooltip("Mental이 가장 낮을 때 패널의 최대 알파값")]
    [SerializeField, Range(0f, 1f)]
    private float maxDarkAlpha = 0.7f;

    [Tooltip("패널 알파값이 변하는 속도")]
    [SerializeField, Min(0f)]
    private float alphaChangeSpeed = 1.5f;

    [Tooltip("패널 크기가 변하는 속도")]
    [SerializeField, Min(0f)]
    private float sizeChangeSpeed = 500f;

    [Header("Mental Text References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform mentalTextLayer;
    [SerializeField] private MentalText[] mentalTexts;

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
    private Vector2 slowTextInterval = new Vector2(5f, 8f);

    [Tooltip("Mental이 매우 낮을 때 텍스트 출력 간격")]
    [SerializeField]
    private Vector2 fastTextInterval = new Vector2(1f, 2.5f);

    [Header("Temporary Mental Text List")]
    [SerializeField]
    private List<string> mediumMentalTexts = new List<string>
    {
        "숲이... 원래 이렇게 어두웠던가?",
        "누군가 따라오고 있어.",
        "뒤를 돌아보지 마."
    };

    [SerializeField]
    private List<string> lowMentalTexts = new List<string>
    {
        "저건 동물이 아니야.",
        "할머니의 목소리를 믿으면 안 돼.",
        "전부 거짓말이야.",
        "도망쳐."
    };

    [Header("Ammo")]
    [SerializeField] private TMP_Text ammoText;

    private Transform playerTransform;

    private float currentMental = 100;
    private float maxMental = 100;

    private EMentalState currentMentalState = EMentalState.High;

    private float targetDarkAlpha;
    private float targetTopBottomSize;
    private float targetSideSize;

    private float currentDarkAlpha;
    private float currentTopBottomSize;
    private float currentSideSize;

    private float mentalTextTimer;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        InitializeReferences();
        InitializeMentalUI();
    }

    private void Start()
    {
        TryFindPlayer();
    }

    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        UpdateMentalScreenEffect();
        UpdateMentalTextSpawner();
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
    }

    private void InitializeMentalUI()
    {
        currentMental = maxMental;
        currentMentalState = EMentalState.High;

        currentDarkAlpha = 0f;
        currentTopBottomSize = minTopBottomSize;
        currentSideSize = minSideSize;

        targetDarkAlpha = 0f;
        targetTopBottomSize = minTopBottomSize;
        targetSideSize = minSideSize;

        if (mentalGauge != null)
        {
            mentalGauge.fillAmount = 1f;
        }

        if (reloadGauge != null)
        {
            reloadGauge.fillAmount = 0f;

            if (hideReloadGaugeWhenIdle)
            {
                reloadGauge.gameObject.SetActive(false);
            }
        }

        UpdateMentalIcon();
        ApplyPanelVisuals();
        ResetMentalTextTimer();
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

        float dangerRatio =
            1f - mentalRatio;

        targetDarkAlpha = Mathf.Lerp(
            0f,
            maxDarkAlpha,
            dangerRatio
        );

        targetTopBottomSize = Mathf.Lerp(
            minTopBottomSize,
            maxTopBottomSize,
            dangerRatio
        );

        targetSideSize = Mathf.Lerp(
            minSideSize,
            maxSideSize,
            dangerRatio
        );

        if (currentMentalState != newMentalState)
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

        maxAmmo = Mathf.Max(0, maxAmmo);

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

    private void UpdateMentalScreenEffect()
    {
        currentDarkAlpha = Mathf.MoveTowards(
            currentDarkAlpha,
            targetDarkAlpha,
            alphaChangeSpeed * Time.deltaTime
        );

        currentTopBottomSize = Mathf.MoveTowards(
            currentTopBottomSize,
            targetTopBottomSize,
            sizeChangeSpeed * Time.deltaTime
        );

        currentSideSize = Mathf.MoveTowards(
            currentSideSize,
            targetSideSize,
            sizeChangeSpeed * Time.deltaTime
        );

        ApplyPanelVisuals();
    }

    private void ApplyPanelVisuals()
    {
        SetImageAlpha(
            topPanel,
            currentDarkAlpha
        );

        SetImageAlpha(
            bottomPanel,
            currentDarkAlpha
        );

        SetImageAlpha(
            leftPanel,
            currentDarkAlpha
        );

        SetImageAlpha(
            rightPanel,
            currentDarkAlpha
        );

        SetPanelHeight(
            topPanel,
            currentTopBottomSize
        );

        SetPanelHeight(
            bottomPanel,
            currentTopBottomSize
        );

        SetPanelWidth(
            leftPanel,
            currentSideSize
        );

        SetPanelWidth(
            rightPanel,
            currentSideSize
        );

        SetSidePanelVerticalPadding(
            leftPanel,
            currentTopBottomSize
        );

        SetSidePanelVerticalPadding(
            rightPanel,
            currentTopBottomSize
        );
    }

    private void SetImageAlpha(
        Image image,
        float alpha
    )
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void SetPanelHeight(
        Image panel,
        float height
    )
    {
        if (panel == null)
        {
            return;
        }

        RectTransform rectTransform =
            panel.rectTransform;

        Vector2 sizeDelta =
            rectTransform.sizeDelta;

        sizeDelta.y = height;

        rectTransform.sizeDelta =
            sizeDelta;
    }

    private void SetPanelWidth(
        Image panel,
        float width
    )
    {
        if (panel == null)
        {
            return;
        }

        RectTransform rectTransform =
            panel.rectTransform;

        Vector2 sizeDelta =
            rectTransform.sizeDelta;

        sizeDelta.x = width;

        rectTransform.sizeDelta =
            sizeDelta;
    }

    private void SetSidePanelVerticalPadding(
        Image panel,
        float padding
    )
    {
        if (panel == null)
        {
            return;
        }

        RectTransform rectTransform =
            panel.rectTransform;

        Vector2 offsetMin =
            rectTransform.offsetMin;

        Vector2 offsetMax =
            rectTransform.offsetMax;

        offsetMin.y = padding;
        offsetMax.y = -padding;

        rectTransform.offsetMin =
            offsetMin;

        rectTransform.offsetMax =
            offsetMax;
    }

    private void UpdateMentalTextSpawner()
    {
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
        MentalText availableText =
            GetAvailableMentalText();

        if (availableText == null)
        {
            return;
        }

        List<string> textList =
            currentMentalState == EMentalState.Low
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

        string message = textList[
            Random.Range(0, textList.Count)
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

    private MentalText GetAvailableMentalText()
    {
        if (mentalTexts == null ||
            mentalTexts.Length == 0)
        {
            return null;
        }

        int startIndex =
            Random.Range(0, mentalTexts.Length);

        for (int i = 0;
             i < mentalTexts.Length;
             i++)
        {
            int index =
                (startIndex + i) %
                mentalTexts.Length;

            MentalText mentalText =
                mentalTexts[index];

            if (mentalText == null)
            {
                continue;
            }

            if (!mentalText.gameObject.activeSelf)
            {
                return mentalText;
            }
        }

        // 모든 MentalText가 표시 중이라면
        // 이번 출력은 건너뜁니다.
        return null;
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
            canvasCamera = canvas.worldCamera;
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
            (float)currentMental /
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
        reloadGauge.gameObject.SetActive(true);
    }


    /// <summary>
    /// 재장전 진행률을 갱신합니다.
    /// normalizedProgress는 0~1 범위입니다.
    /// </summary>
    public void UpdateReloadGauge(float normalizedProgress)
    {
        if (reloadGauge == null)
        {
            return;
        }

        reloadGauge.fillAmount = Mathf.Clamp01(
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
            reloadGauge.gameObject.SetActive(false);
        }
    }
}
