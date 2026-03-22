using UnityEngine;
using TMPro;

public class SpeechBubble : MonoBehaviour
{
    [Header("Text Settings")]
    public TMP_Text textComponent;
    public float maxWidth = 200f;
    public float padding = 10f;

    // 말풍선 애니메이션 효과 설정값
    [Header("Animation Settings")]
    public bool enableAnimation = true;
    public float animationDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        if (enableAnimation)
        {
            PlayShowAnimation();
        }
    }

    void InitializeComponents()
    {
        // 컴포넌트 자동 찾기
        if (textComponent == null)
        {
            textComponent = GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                Debug.Log($"[SpeechBubble] Text 컴포넌트 발견: {textComponent.name}");
            }
            else
            {
                Debug.LogWarning("[SpeechBubble] Text 컴포넌트를 찾을 수 없습니다. 수동으로 할당하세요.");
            }
        }

        rectTransform = GetComponent<RectTransform>();

        // CanvasGroup 추가
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void SetText(string message)
    {
        if (textComponent == null)
        {
            Debug.LogWarning("[SpeechBubble] 텍스트 컴포넌트가 없습니다.");
            return;
        }

        textComponent.text = message;

        // 텍스트 크기에 맞춰 말풍선 크기 조정
        StartCoroutine(AdjustBubbleSize());
    }

    System.Collections.IEnumerator AdjustBubbleSize()
    {
        // 텍스트 레이아웃 업데이트 대기
        yield return null;

        if (textComponent != null && rectTransform != null)
        {
            // 텍스트 크기 가져오기
            Vector2 textSize = textComponent.GetPreferredValues(maxWidth, float.MaxValue);

            // 패딩 추가
            Vector2 bubbleSize = new Vector2(
                Mathf.Min(textSize.x + padding * 2, maxWidth + padding * 2),
                textSize.y + padding * 2
            );

            // 말풍선 크기 설정
            rectTransform.sizeDelta = bubbleSize;

            Debug.Log($"[SpeechBubble] 말풍선 크기 조정: {bubbleSize}");
        }
    }

    void PlayShowAnimation()
    {
        if (rectTransform == null || canvasGroup == null) return;

        // 초기 상태 설정
        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        // 애니메이션 시작
        StartCoroutine(AnimateShow());
    }

    System.Collections.IEnumerator AnimateShow()
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // 스케일 애니메이션
            float scaleValue = scaleCurve.Evaluate(progress);
            rectTransform.localScale = Vector3.one * scaleValue;

            // 알파 애니메이션
            canvasGroup.alpha = progress;

            yield return null;
        }

        // 최종 상태 설정
        rectTransform.localScale = Vector3.one;
        canvasGroup.alpha = 1f;
    }

    public void PlayHideAnimation(System.Action onComplete = null)
    {
        StartCoroutine(AnimateHide(onComplete));
    }

    System.Collections.IEnumerator AnimateHide(System.Action onComplete = null)
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // 스케일 애니메이션 (역방향)
            float scaleValue = scaleCurve.Evaluate(1f - progress);
            rectTransform.localScale = Vector3.one * scaleValue;

            // 알파 애니메이션 (역방향)
            canvasGroup.alpha = 1f - progress;

            yield return null;
        }

        // 최종 상태 설정
        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        // 완료 콜백 호출
        onComplete?.Invoke();
    }

    // 즉시 숨기기 (애니메이션X)
    public void HideImmediate()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    public void SetMaxWidth(float width)
    {
        maxWidth = width;
        if (!string.IsNullOrEmpty(textComponent?.text))
        {
            StartCoroutine(AdjustBubbleSize());
        }
    }

    public void SetPadding(float paddingValue)
    {
        padding = paddingValue;
        if (!string.IsNullOrEmpty(textComponent?.text))
        {
            StartCoroutine(AdjustBubbleSize());
        }
    }

    public void SetAnimationEnabled(bool enabled)
    {
        enableAnimation = enabled;
    }
}