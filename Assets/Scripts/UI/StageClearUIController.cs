using TMPro;
using UnityEngine;

public class StageClearUIController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField]
    private GameObject exitButton;

    [SerializeField]
    private GameObject nextButton;

    [Header("Red Images")]
    [SerializeField]
    private GameObject redStageClear;

    [SerializeField]
    private GameObject redGameClear;

    [Header("Message")]
    [SerializeField]
    private TMP_Text messageText;

    [Header("Messages")]
    [SerializeField, TextArea(3, 6)]
    private string stageClearMessage =
        "\"끝난 건 아니지만, 적어도 지금은 괜찮아.\n" +
        "숨을 고르고 길을 다시 확인해야 해.\n" +
        "다음에는 더 조심하자.\"";

    [SerializeField, TextArea(3, 6)]
    private string victoryMessage =
        "\"이제는 웃어도 괜찮을 것 같아.\n" +
        "숲도, 늑대도, 무서운 목소리도 \n모두 지나간 일이야.\n" +
        "오늘부터는 새로운 이야기가 시작될 거야.\"";

    private void OnEnable()
    {
        RefreshUI();
    }

    /// <summary>
    /// 현재 GameManager 상태에 맞게 결과 UI를 갱신합니다.
    /// </summary>
    public void RefreshUI()
    {
        DisableAllStateObjects();

        if (!GameManager.HasInstance)
        {
            Debug.LogWarning(
                $"{nameof(StageClearUIController)}: " +
                "GameManager를 찾을 수 없습니다.",
                this
            );

            return;
        }

        switch (GameManager.Instance.CurrentState)
        {
            case EGameState.StageClear:
                ShowStageClearUI();
                break;

            case EGameState.Victory:
                ShowVictoryUI();
                break;

            default:
                Debug.LogWarning(
                    $"{nameof(StageClearUIController)}: " +
                    $"지원하지 않는 게임 상태입니다. " +
                    $"현재 상태: {GameManager.Instance.CurrentState}",
                    this
                );
                break;
        }
    }

    private void ShowStageClearUI()
    {
        SetActive(nextButton, true);
        SetActive(redStageClear, true);

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = stageClearMessage;
        }
    }

    private void ShowVictoryUI()
    {
        SetActive(exitButton, true);
        SetActive(redGameClear, true);

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = victoryMessage;
        }
    }

    private void DisableAllStateObjects()
    {
        SetActive(exitButton, false);
        SetActive(nextButton, false);
        SetActive(redStageClear, false);
        SetActive(redGameClear, false);

        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
            messageText.text = string.Empty;
        }
    }

    private void SetActive(
        GameObject target,
        bool isActive
    )
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}