using UnityEngine;
using UnityEngine.UI;

public class QuitUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject quitUIPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private bool isQuitUIActive = false;
    private PlayerInputHandler playerInputHandler;

    void Awake()
    {
        ValidateReferences();
        SetupButtons();
    }

    void Start()
    {
        // 초기에는 QuitUI를 비활성화
        HideQuitUI();

        // 로컬 플레이어의 PlayerInputHandler 찾기
        FindPlayerInputHandler();
    }

    void Update()
    {
        HandleESCInput();
    }

    private void ValidateReferences()
    {
        if (quitUIPanel == null)
        {
            Debug.LogError("[QuitUIManager] QuitUI Panel이 등록되지 않았습니다!");
        }

        if (yesButton == null)
        {
            Debug.LogError("[QuitUIManager] Yes Button이 등록되지 않았습니다!");
        }

        if (noButton == null)
        {
            Debug.LogError("[QuitUIManager] No Button이 등록되지 않았습니다!");
        }
    }

    private void SetupButtons()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(OnYesButtonClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(OnNoButtonClicked);
        }
    }

    private void FindPlayerInputHandler()
    {
        // 로컬 플레이어의 InputHandler 찾기
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            if (player.isLocalPlayer)
            {
                playerInputHandler = player.GetComponent<PlayerInputHandler>();
                if (playerInputHandler != null)
                {
                    Debug.Log("[QuitUIManager] 로컬 플레이어의 InputHandler 인식 완료");
                }
                break;
            }
        }

        if (playerInputHandler == null)
        {
            Debug.LogWarning("[QuitUIManager] PlayerInputHandler를 찾지 못했습니다!");
        }
    }

    private void HandleESCInput()
    {
        // QuitUI가 활성화되어 있을 때는 ESC 입력 무시
        if (isQuitUIActive) return;

        // ESC 키 입력 확인
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowQuitUI();
        }
    }

    public void ShowQuitUI()
    {
        if (quitUIPanel == null) return;

        quitUIPanel.SetActive(true);
        isQuitUIActive = true;

        // 로컬 플레이어만 입력 비활성화 (다른 플레이어는 계속 움직임)
        DisablePlayerInput();

        Debug.Log("[QuitUIManager] QuitUI 보이기");
    }

    public void HideQuitUI()
    {
        if (quitUIPanel == null) return;

        quitUIPanel.SetActive(false);
        isQuitUIActive = false;

        // 로컬 플레이어 입력 활성화
        EnablePlayerInput();

        Debug.Log("[QuitUIManager] QuitUI 가리기");
    }

    private void DisablePlayerInput()
    {
        // PlayerController의 이동 정지
        if (playerInputHandler != null)
        {
            PlayerController controller = playerInputHandler.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetCanMove(false);
                // 현재 움직임도 즉시 정지
                controller.SetCurrentMovement(Vector2.zero);
            }
        }
    }

    private void EnablePlayerInput()
    {
        // PlayerController의 이동 재개
        if (playerInputHandler != null)
        {
            PlayerController controller = playerInputHandler.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetCanMove(true);
            }
        }
    }

    private void OnYesButtonClicked()
    {
        Debug.Log("[QuitUIManager] 종료 버튼이 눌렸습니다. 게임을 종료합니다.");

        // GameManager를 통해 종료
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.QuitGame();
        }
        else
        {
            // GameManager가 없으면 직접 종료
            QuitGame();
        }
    }

    private void OnNoButtonClicked()
    {
        Debug.Log("[QuitUIManager] 재개 버튼이 눌렸습니다. 게임으로 돌아갑니다.");
        HideQuitUI();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public bool IsQuitUIActive()
    {
        return isQuitUIActive;
    }

    void OnDestroy()
    {
        EnablePlayerInput();
    }
}