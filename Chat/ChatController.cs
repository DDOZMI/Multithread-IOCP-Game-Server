using UnityEngine;
using System.Collections.Generic;
using ChatSystem;

public class ChatController : MonoBehaviour
{
    public static ChatController Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private NetworkClient networkClient;

    private IChatInputHandler inputHandler;
    private ISpeechBubbleManager bubbleManager;

    // 플레이어 관리
    private PlayerController localPlayer;
    private Dictionary<int, GameObject> playerRegistry = new Dictionary<int, GameObject>();

    // 네트워크 상태
    private bool isNetworkAvailable = false;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeComponents();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void Start()
    {
        ValidateNetworkClient();
        FindLocalPlayer();
    }

    private void Update()
    {
        // 로컬 플레이어가 없으면 계속 찾기
        if (localPlayer == null)
        {
            FindLocalPlayer();
        }
    }

    private void InitializeComponents()
    {
        // 컴포넌트 찾기 또는 생성
        inputHandler = GetComponent<IChatInputHandler>() ?? gameObject.AddComponent<ChatInputHandler>();

        bubbleManager = GetComponent<ISpeechBubbleManager>() ?? gameObject.AddComponent<SpeechBubbleManager>();

        Debug.Log($"[ChatController] [{GetType().Name}] 컴포넌트 초기화");
    }

    // 채팅 입력, 네트워크 Event subscribe
    private void SubscribeToEvents()
    {
        ChatEvents.OnChatSubmitted += HandleChatSubmitted;
        ChatEvents.OnChatMessageReceived += HandleChatReceived;
    }

    private void UnsubscribeFromEvents()
    {
        ChatEvents.OnChatSubmitted -= HandleChatSubmitted;
        ChatEvents.OnChatMessageReceived -= HandleChatReceived;
    }

    // 네트워크 객체 찾기
    private void ValidateNetworkClient()
    {
        if (networkClient == null)
        {
            networkClient = FindFirstObjectByType<NetworkClient>();
        }

        if (networkClient != null)
        {
            isNetworkAvailable = true;
            Debug.Log($"[ChatController] [{GetType().Name}] NetworkClient를 찾았습니다.");
        }
        else
        {
            isNetworkAvailable = false;
            Debug.LogWarning($"[ChatController] [{GetType().Name}] NetworkClient를 찾을 수 없습니다!");
        }
    }

    // 로컬 플레이어 찾기
    private void FindLocalPlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            if (player.isLocalPlayer)
            {
                localPlayer = player;
                RegisterPlayer(GetLocalPlayerId(), player.gameObject);
                Debug.Log($"[ChatController] [{GetType().Name}] 로컬 플레이어 확인: {player.name}");
                break;
            }
        }
    }

    private int GetLocalPlayerId()
    {
        return networkClient != null ? networkClient.myPlayerId : -1;
    }

    // 채팅 입력 처리
    private void HandleChatSubmitted(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        Debug.Log($"[ChatController] [{GetType().Name}] 채팅: {message}");

        if (isNetworkAvailable && networkClient != null)
        {
            // 네트워크로 전송
            networkClient.SendChatMessage(message);
            Debug.Log($"[ChatController] [{GetType().Name}] 채팅이 전송됩니다.");
        }
        else
        {
            // 네트워크가 없으면 로컬 표시만
            ShowLocalChatBubble(message);
            Debug.LogWarning($"[ChatController] [{GetType().Name}] 네트워크를 사용할 수 없습니다. 로컬 플레이어 말풍선만 띄웁니다.");
        }
    }

    // 네트워크 채팅 수신 처리
    private void HandleChatReceived(ChatMessageData data)
    {
        Debug.Log($"[ChatController] [{GetType().Name}] 플레이어 {data.PlayerId}로부터 메세지를 받았습니다: {data.Message}");

        // 플레이어 객체 찾기
        GameObject playerObject = FindPlayerObject(data.PlayerId);

        if (playerObject != null)
        {

        }
        else
        {
            Debug.LogWarning($"[ChatController] [{GetType().Name}] 플레이어 오브젝트를 찾을 수 없습니다 (ID: {data.PlayerId})");
        }
    }

    // 로컬 클라이언트 말풍선 처리
    private void ShowLocalChatBubble(string message)
    {
        if (localPlayer == null)
        {
            Debug.LogWarning($"[ChatController] [{GetType().Name}] 로컬 말풍선을 띄울 수 없습니다. 로컬 플레이어를 찾을 수 없습니다.");
            return;
        }

        // 로컬 채팅 메시지 이벤트 발행
        ChatMessageData data = new ChatMessageData(
            GetLocalPlayerId(),
            message,
            localPlayer.gameObject
        );

        ChatEvents.RaiseChatMessageReceived(data);
    }

    private GameObject FindPlayerObject(int playerId)
    {
        // 등록된 플레이어에서 찾기
        if (playerRegistry.TryGetValue(playerId, out GameObject player))
        {
            return player;
        }

        // 로컬 플레이어 확인
        if (playerId == GetLocalPlayerId() && localPlayer != null)
        {
            return localPlayer.gameObject;
        }

        // NetworkClient의 다른 플레이어 목록에서 찾기
        return null;
    }

    // 플레이어 등록
    public void RegisterPlayer(int playerId, GameObject playerObject)
    {
        if (playerObject != null)
        {
            playerRegistry[playerId] = playerObject;
            Debug.Log($"[ChatController] [{GetType().Name}] 플레이어 {playerId} 이(가) 등록되었습니다.");
        }
    }

    // 플레이어 등록 해제
    public void UnregisterPlayer(int playerId)
    {
        if (playerRegistry.Remove(playerId))
        {
            Debug.Log($"[ChatController] [{GetType().Name}] 플레이어 {playerId} 의 등록이 해제되었습니다.");
        }
    }

    // 채팅 패널 활성화 상태 확인
    public bool IsChatPanelActive()
    {
        return inputHandler?.IsChatInputActive ?? false;
    }

    // 채팅 패널 활성화/비활성화
    public void SetChatPanelActive(bool active)
    {
        if (inputHandler != null)
        {
            if (active)
                inputHandler.ShowChatInput();
            else
                inputHandler.HideChatInput();
        }
    }

    // 모든 말풍선 제거
    public void ClearAllSpeechBubbles()
    {
        bubbleManager?.ClearAllBubbles();
    }

    // 특정 플레이어의 말풍선 숨기기
    public void HideSpeechBubbleForPlayer(GameObject player)
    {
        bubbleManager?.HideSpeechBubble(player);
    }

    private void OnDestroy()
    {
        ClearAllSpeechBubbles();
        playerRegistry.Clear();
    }
}