using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MentalText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text messageText;

    [Header("Lifetime")]
    [SerializeField, Min(0f)] private float visibleDuration = 2f;

    private CanvasGroup canvasGroup;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (messageText == null)
        {
            messageText = GetComponent<TMP_Text>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        ShowImmediately();
    }

    private void OnDisable()
    {
        StopHideCoroutine();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// 문구를 설정하고 오브젝트를 활성화합니다.
    /// 이미 활성화된 상태라면 표시 시간을 초기화합니다.
    /// </summary>
    public void Show(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        if (gameObject.activeSelf)
        {
            ShowImmediately();
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    private void ShowImmediately()
    {
        StopHideCoroutine();

        canvasGroup.alpha = 1f;
        hideCoroutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        yield return new WaitForSeconds(visibleDuration);

        canvasGroup.alpha = 0f;
        hideCoroutine = null;

        gameObject.SetActive(false);
    }

    private void StopHideCoroutine()
    {
        if (hideCoroutine == null)
        {
            return;
        }

        StopCoroutine(hideCoroutine);
        hideCoroutine = null;
    }
}