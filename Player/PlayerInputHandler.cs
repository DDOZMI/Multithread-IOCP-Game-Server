using UnityEngine;
using System.Collections.Generic;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    public bool enableCommandPattern = true;

    // 키 매핑 설정
    private Dictionary<KeyCode, Vector2> keyToDirectionMap;

    private PlayerController playerController;
    private CommandFactory commandFactory;
    private ChatController chatController;
    private QuitUIManager quitUIManager;

    // 입력 상태
    private Vector2 currentMovementInput;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[PlayerInputHandler] PlayerController를 찾지 못했습니다!");
            return;
        }

        // Command Pattern 초기화
        if (enableCommandPattern)
        {
            InitializeCommandSystem();
        }

        // 키 매핑 초기화
        InitializeKeyMappings();

        // ChatController 찾기
        chatController = ChatController.Instance;
        if (chatController == null)
        {
            Debug.LogWarning("[PlayerInputHandler] ChatController를 찾지 못했습니다! 채팅 기능을 지원할 수 없습니다.");
        }

        // QuitUIManager 찾기 (추가)
        quitUIManager = FindFirstObjectByType<QuitUIManager>();
        if (quitUIManager == null)
        {
            Debug.LogWarning("[PlayerInputHandler] QuitUIManager를 찾지 못했습니다! 종료UI를 지원할 수 없습니다.");
        }
    }

    void InitializeCommandSystem()
    {
        commandFactory = new CommandFactory(transform, playerController);
    }

    void InitializeKeyMappings()
    {
        keyToDirectionMap = new Dictionary<KeyCode, Vector2>
        {
            { KeyCode.W, Vector2.up },
            { KeyCode.UpArrow, Vector2.up },
            { KeyCode.S, Vector2.down },
            { KeyCode.DownArrow, Vector2.down },
            { KeyCode.A, Vector2.left },
            { KeyCode.LeftArrow, Vector2.left },
            { KeyCode.D, Vector2.right },
            { KeyCode.RightArrow, Vector2.right }
        };
    }

    void Update()
    {
        // 로컬 플레이어만 입력 처리
        if (!playerController.isLocalPlayer) return;

        // QuitUI가 활성화되어 있으면 입력 무시 (추가)
        if (quitUIManager != null && quitUIManager.IsQuitUIActive())
        {
            playerController.SetCurrentMovement(Vector2.zero);
            return;
        }

        // 채팅 패널이 활성화되어 있으면 이동 입력 무시
        if (chatController != null && chatController.IsChatPanelActive())
        {
            // 채팅 중일 때는 이동 정지
            playerController.SetCurrentMovement(Vector2.zero);
            return;
        }

        HandleMovementInput();
    }

    void HandleMovementInput()
    {
        // 입력 수집
        Vector2 inputDirection = GetMovementInput();

        if (enableCommandPattern)
        {
            HandleMovementWithCommands(inputDirection);
        }
        else
        {
            HandleMovementDirect(inputDirection);
        }
    }

    Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;

        // 키보드 입력 처리
        foreach (var keyMapping in keyToDirectionMap)
        {
            if (Input.GetKey(keyMapping.Key))
            {
                input += keyMapping.Value;
            }
        }

        // 또는 Unity Input System 사용
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (input == Vector2.zero && (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f))
        {
            input = new Vector2(horizontal, vertical);
        }

        // 대각선 이동 시 정규화
        if (input.magnitude > 1f)
        {
            input = input.normalized;
        }

        currentMovementInput = input;
        return input;
    }

    void HandleMovementWithCommands(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            ICommand moveCommand = commandFactory.CreateMoveCommand(direction, Time.deltaTime);
            if (moveCommand != null)
            {
                moveCommand.Execute();

                // PlayerController에 현재 이동 정보 전달 (애니메이션 등을 위해)
                playerController.SetCurrentMovement(direction);
            }
        }
        else
        {
            playerController.SetCurrentMovement(Vector2.zero);
        }
    }

    void HandleMovementDirect(Vector2 direction)
    {
        // 기존 방식으로 PlayerController에 입력 전달
        playerController.SetCurrentMovement(direction);
    }

    // 공개 메서드들
    public void SetCommandPatternEnabled(bool enabled)
    {
        enableCommandPattern = enabled;
        if (enabled && commandFactory == null)
        {
            InitializeCommandSystem();
        }
    }

    // 채팅 중인지 확인하는 메서드
    public bool IsInChatMode()
    {
        return chatController != null && chatController.IsChatPanelActive();
    }

    // QuitUI가 활성화되어 있는지 확인하는 메서드 (추가)
    public bool IsInQuitMode()
    {
        return quitUIManager != null && quitUIManager.IsQuitUIActive();
    }
}