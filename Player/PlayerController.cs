using UnityEngine;

public class PlayerController : MonoBehaviour, IPlayerComponent
{
    // 플레이어 이동속도 설정
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    // 로컬/원격 플레이어 구분 및 네트워크 전송 주기 설정
    [Header("Network Settings")]
    public bool isLocalPlayer = false;
    public float sendRate = 0.05f;

    // 원격 플레이어 움직임 보간속도 설정
    [Header("Interpolation Settings")]
    public float interpolationSpeed = 10f;

    // 유니티 컴포넌트
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerInputHandler inputHandler;

    // 플레이어 컴포넌트
    private IPlayerMovement movementComponent;
    private IPlayerNetwork networkComponent;
    private IPlayerAnimation animationComponent;

    public Transform Transform => transform;
    public bool IsLocalPlayer => isLocalPlayer;
    public float MoveSpeed => moveSpeed;
    public Vector2 CurrentMovement => movementComponent?.CurrentMovement ?? Vector2.zero;
    public Vector2 LastMoveDirection => movementComponent?.LastMoveDirection ?? Vector2.down;
    public Rigidbody2D Rigidbody => rb;
    public Animator Animator => animator;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    void Start()
    {
        InitializeComponents();
        SetupRigidbody();
        InitializeComponentPattern();
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 로컬 플레이어에게만 InputHandler 추가
        if (isLocalPlayer)
        {
            inputHandler = gameObject.GetComponent<PlayerInputHandler>();
            if (inputHandler == null)
            {
                inputHandler = gameObject.AddComponent<PlayerInputHandler>();
            }
        }
    }

    void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
    }

    void InitializeComponentPattern()
    {
        // 컴포넌트들을 초기화
        movementComponent = new PlayerMovementComponent(this);
        networkComponent = new PlayerNetworkComponent(this, movementComponent, sendRate, interpolationSpeed);
        animationComponent = new PlayerAnimationComponent(this, networkComponent);
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            networkComponent.CheckAndSendPosition();
        }
        else
        {
            networkComponent.HandleRemotePlayerMovement();
        }

        animationComponent.UpdateAnimation();
    }

    void FixedUpdate()
    {
        movementComponent.HandleMovement();
    }

    public void SetCurrentMovement(Vector2 movement)
    {
        movementComponent.SetCurrentMovement(movement);
    }

    public void SetNetworkPosition(float x, float y)
    {
        networkComponent.SetNetworkPosition(x, y);
    }

    public void SetPosition(float x, float y)
    {
        // Start()호출 전인 경우 networkComponent가 null이더라도 prefab의 위치는 설정해두도록 함.
        // 쉽게 말해 서버에 연결되기 전에도 플레이어가 생성될 위치는 정해두는 것.
        if (networkComponent == null)
        {
            transform.position = new Vector3(x, y, transform.position.z);
            return;
        }

        // Start()가 실행된 이후에는 정상적으로 networkComponent를 통해 위치를 설정.
        networkComponent.SetPosition(x, y);
    }

    public Vector2 GetFacingDirection()
    {
        return movementComponent.GetFacingDirection();
    }

    public void SetCanMove(bool canMove)
    {
        movementComponent.SetCanMove(canMove);
    }

    public void PredictiveMove(float deltaTime)
    {
        networkComponent.PredictiveMove(deltaTime);
    }

    public SpriteRenderer GetSpriteRenderer() => spriteRenderer;
    public Rigidbody2D GetRigidbody2D() => rb;
    public Animator GetAnimator() => animator;

    public IPlayerMovement GetMovementComponent() => movementComponent;
    public IPlayerNetwork GetNetworkComponent() => networkComponent;
    public IPlayerAnimation GetAnimationComponent() => animationComponent;
}