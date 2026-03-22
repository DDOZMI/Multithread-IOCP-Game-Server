using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ChatSystem;

public class SpeechBubbleManager : MonoBehaviour, ISpeechBubbleManager
{
    [Header("Bubble Settings")]
    [SerializeField] private GameObject speechBubblePrefab;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private float bubbleDisplayTime = 3f;
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0, 1.0f, 0);

    [Header("Update Settings")]
    [SerializeField] private float positionUpdateInterval = 0.02f; // 50fps

    // 플레이어별 말풍선 및 코루틴 관리
    private Dictionary<GameObject, GameObject> playerBubbles = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, Coroutine> hideCoroutines = new Dictionary<GameObject, Coroutine>();
    private Dictionary<GameObject, Coroutine> updateCoroutines = new Dictionary<GameObject, Coroutine>();

    // Factory 패턴 적용
    private ISpeechBubbleFactory bubbleFactory;

    private void Awake()
    {
        ValidateReferences();
        InitializeFactory();
    }

    private void OnEnable()
    {
        // 채팅 메시지 수신 이벤트 구독
        ChatEvents.OnChatMessageReceived += OnChatMessageReceived;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        ChatEvents.OnChatMessageReceived -= OnChatMessageReceived;
    }

    private void OnDestroy()
    {
        ClearAllBubbles();
    }

    private void ValidateReferences()
    {
        if (speechBubblePrefab == null)
        {
            Debug.LogError($"[SpeechBubbleManager] [{GetType().Name}] 말풍선 prefab이 등록되지 않았습니다!");
        }

        if (uiCanvas == null)
        {
            Debug.LogWarning($"[SpeechBubbleManager] [{GetType().Name}] UI Canvas가 등록되지 않았습니다!");
            uiCanvas = FindFirstObjectByType<Canvas>();

            if (uiCanvas == null)
            {
                Debug.LogError($"[SpeechBubbleManager] [{GetType().Name}] Scene에서 캔버스를 찾을 수 없습니다!");
            }
        }
    }

    private void InitializeFactory()
    {
        // Factory 구현체 생성
        bubbleFactory = new DefaultSpeechBubbleFactory(speechBubblePrefab);
    }

    private void OnChatMessageReceived(ChatMessageData data)
    {
        if (data.PlayerObject != null)
        {
            ShowSpeechBubble(data.PlayerObject, data.Message);
        }
        else
        {
            Debug.LogWarning($"[SpeechBubbleManager] [{GetType().Name}] 말풍선을 표시할 수 없습니다. 플레이어 오브젝트가 {data.PlayerId}에 없습니다.");
        }
    }

    public void ShowSpeechBubble(GameObject player, string message)
    {
        if (!CanShowBubble(player, message)) return;

        // 기존 말풍선 정리
        CleanupExistingBubble(player);

        // 새 말풍선 생성
        GameObject newBubble = CreateBubble(message);
        if (newBubble == null) return;

        // 말풍선 등록 및 위치 설정
        RegisterBubble(player, newBubble);
        UpdateBubblePosition(newBubble, player);

        // 코루틴 시작
        StartBubbleCoroutines(player, newBubble);

        Debug.Log($"[{GetType().Name}] Speech bubble shown for {player.name}: {message}");
    }

    private bool CanShowBubble(GameObject player, string message)
    {
        if (player == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Cannot show bubble - player is null");
            return false;
        }

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"[{GetType().Name}] Cannot show bubble - message is empty");
            return false;
        }

        if (uiCanvas == null)
        {
            Debug.LogError($"[{GetType().Name}] Cannot show bubble - Canvas is not set!");
            return false;
        }

        return true;
    }

    private void CleanupExistingBubble(GameObject player)
    {
        // 기존 말풍선 제거
        if (playerBubbles.TryGetValue(player, out GameObject existingBubble))
        {
            if (existingBubble != null)
            {
                Destroy(existingBubble);
            }
            playerBubbles.Remove(player);
        }

        // 기존 코루틴 중지
        StopExistingCoroutines(player);
    }

    private void StopExistingCoroutines(GameObject player)
    {
        if (hideCoroutines.TryGetValue(player, out Coroutine hideCoroutine))
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
            hideCoroutines.Remove(player);
        }

        if (updateCoroutines.TryGetValue(player, out Coroutine updateCoroutine))
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            updateCoroutines.Remove(player);
        }
    }

    private GameObject CreateBubble(string message)
    {
        if (bubbleFactory == null)
        {
            Debug.LogError($"[SpeechBubbleManager] [{GetType().Name}] 말풍선 팩토리가 초기화되지 않았습니다.");
            return null;
        }

        GameObject bubble = bubbleFactory.CreateSpeechBubble(message, uiCanvas.transform);

        if (bubble != null)
        {
            bubbleFactory.ConfigureBubble(bubble, message);
        }

        return bubble;
    }

    private void RegisterBubble(GameObject player, GameObject bubble)
    {
        playerBubbles[player] = bubble;
    }

    private void StartBubbleCoroutines(GameObject player, GameObject bubble)
    {
        // 위치 업데이트 코루틴
        Coroutine updateCoroutine = StartCoroutine(UpdateBubblePositionCoroutine(bubble, player));
        updateCoroutines[player] = updateCoroutine;

        // 자동 숨김 코루틴
        Coroutine hideCoroutine = StartCoroutine(AutoHideBubbleCoroutine(player, bubble));
        hideCoroutines[player] = hideCoroutine;
    }

    private void UpdateBubblePosition(GameObject bubble, GameObject player)
    {
        if (bubble == null || player == null) return;

        RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
        if (bubbleRect == null) return;

        // 월드 좌표 계산
        Vector3 worldPosition = player.transform.position + bubbleOffset;

        // 스크린 좌표로 변환
        Camera cam = uiCanvas.worldCamera ?? Camera.main;
        if (cam == null) return;

        Vector3 screenPoint = cam.WorldToScreenPoint(worldPosition);

        // Canvas 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.transform as RectTransform,
            screenPoint,
            cam,
            out Vector2 localPoint);

        bubbleRect.localPosition = localPoint;
    }

    private IEnumerator UpdateBubblePositionCoroutine(GameObject bubble, GameObject player)
    {
        while (bubble != null && player != null)
        {
            UpdateBubblePosition(bubble, player);
            yield return new WaitForSeconds(positionUpdateInterval);
        }

        // 정리
        if (updateCoroutines.ContainsKey(player))
        {
            updateCoroutines.Remove(player);
        }
    }

    private IEnumerator AutoHideBubbleCoroutine(GameObject player, GameObject bubble)
    {
        yield return new WaitForSeconds(bubbleDisplayTime);

        if (bubble != null)
        {
            // 애니메이션이 있는 경우 처리
            SpeechBubble bubbleComponent = bubble.GetComponent<SpeechBubble>();
            if (bubbleComponent != null)
            {
                bubbleComponent.PlayHideAnimation(() => DestroyBubble(player, bubble));
            }
            else
            {
                DestroyBubble(player, bubble);
            }
        }
    }

    private void DestroyBubble(GameObject player, GameObject bubble)
    {
        if (bubble != null)
        {
            Destroy(bubble);
        }

        // 딕셔너리 정리
        playerBubbles.Remove(player);
        hideCoroutines.Remove(player);

        // 업데이트 코루틴도 중지
        if (updateCoroutines.TryGetValue(player, out Coroutine updateCoroutine))
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            updateCoroutines.Remove(player);
        }
    }

    public void HideSpeechBubble(GameObject player)
    {
        if (player == null) return;

        CleanupExistingBubble(player);

        Debug.Log($"[SpeechBubbleManager] [{GetType().Name}] 말풍선이 가려졌습니다 - {player.name}");
    }

    public void ClearAllBubbles()
    {
        // 모든 말풍선 제거
        foreach (var kvp in playerBubbles)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        playerBubbles.Clear();

        // 모든 코루틴 중지
        foreach (var coroutine in hideCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        hideCoroutines.Clear();

        foreach (var coroutine in updateCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        updateCoroutines.Clear();

        Debug.Log($"[SpeechBubbleManager] [{GetType().Name}] 모든 말풍선을 지웠습니다.");
    }

    public void SetBubbleDisplayTime(float time)
    {
        bubbleDisplayTime = Mathf.Max(0.1f, time);
    }

    public void SetBubbleOffset(Vector3 offset)
    {
        bubbleOffset = offset;
    }

    public void SetCanvas(Canvas canvas)
    {
        uiCanvas = canvas;
    }
}

// 기본 말풍선 Factory 구현
public class DefaultSpeechBubbleFactory : ISpeechBubbleFactory
{
    private GameObject prefab;

    public DefaultSpeechBubbleFactory(GameObject speechBubblePrefab)
    {
        prefab = speechBubblePrefab;
    }

    public GameObject CreateSpeechBubble(string message, Transform parent)
    {
        if (prefab == null)
        {
            Debug.LogError("[SpeechBubbleManager] 말풍선 prefab이 없습니다.");
            return null;
        }

        return GameObject.Instantiate(prefab, parent);
    }

    public void ConfigureBubble(GameObject bubble, string message)
    {
        if (bubble == null) return;

        SpeechBubble bubbleComponent = bubble.GetComponent<SpeechBubble>();
        if (bubbleComponent != null)
        {
            bubbleComponent.SetText(message);
        }
    }
}