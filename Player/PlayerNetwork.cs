using UnityEngine;

public class PlayerNetworkComponent : IPlayerNetwork
{
    private readonly IPlayerComponent playerComponent;
    private readonly IPlayerMovement movementComponent;

    private NetworkClient networkClient;
    private float sendRate;
    private float interpolationSpeed;

    private float lastSendTime = 0f;
    private Vector3 lastSentPosition;

    // 원격 플레이어 관련 변수
    private Vector3 targetPosition;
    private Vector3 previousPosition;
    private bool hasTargetPosition = false;

    public PlayerNetworkComponent(IPlayerComponent playerComponent, IPlayerMovement movementComponent,
                                float sendRate, float interpolationSpeed)
    {
        this.playerComponent = playerComponent;
        this.movementComponent = movementComponent;
        this.sendRate = sendRate;
        this.interpolationSpeed = interpolationSpeed;

        SetupNetworking();
    }

    private void SetupNetworking()
    {
        if (playerComponent.IsLocalPlayer)
        {
            networkClient = Object.FindFirstObjectByType<NetworkClient>();
            if (networkClient == null)
            {
                Debug.LogError("NetworkClient를 찾지 못했습니다! Scene에 등록되어 있는지 확인하세요.");
            }
            lastSentPosition = playerComponent.Transform.position;
        }
        else
        {
            targetPosition = playerComponent.Transform.position;
            previousPosition = playerComponent.Transform.position;
        }
    }

    public void CheckAndSendPosition()
    {
        if (networkClient == null || !playerComponent.IsLocalPlayer) return;

        bool timeToSend = Time.time - lastSendTime >= sendRate;
        bool positionChanged = Vector3.Distance(playerComponent.Transform.position, lastSentPosition) > 0.1f;

        if (timeToSend && (positionChanged || playerComponent.CurrentMovement.magnitude > 0))
        {
            networkClient.SendPlayerMove(playerComponent.Transform.position.x, playerComponent.Transform.position.y);
            lastSendTime = Time.time;
            lastSentPosition = playerComponent.Transform.position;
        }
    }

    public void SetNetworkPosition(float x, float y)
    {
        if (playerComponent.IsLocalPlayer) return;

        previousPosition = hasTargetPosition ? targetPosition : playerComponent.Transform.position;
        targetPosition = new Vector3(x, y, playerComponent.Transform.position.z);
        hasTargetPosition = true;
    }

    public void SetPosition(float x, float y)
    {
        Vector3 newPos = new Vector3(x, y, playerComponent.Transform.position.z);
        playerComponent.Transform.position = newPos;

        if (!playerComponent.IsLocalPlayer)
        {
            targetPosition = newPos;
            previousPosition = newPos;
            hasTargetPosition = true;
        }
    }

    public void HandleRemotePlayerMovement()
    {
        if (playerComponent.IsLocalPlayer || !hasTargetPosition) return;

        playerComponent.Transform.position = Vector3.Lerp(playerComponent.Transform.position, targetPosition,
                                        interpolationSpeed * Time.deltaTime);

        Vector3 moveDirection = targetPosition - previousPosition;
        if (Mathf.Abs(moveDirection.x) > 0.01f)
        {
            playerComponent.SpriteRenderer.flipX = moveDirection.x < 0;
        }
    }

    public void PredictiveMove(float deltaTime)
    {
        if (playerComponent.IsLocalPlayer || !hasTargetPosition) return;

        Vector3 velocity = (targetPosition - previousPosition) / deltaTime;
        Vector3 predictedPosition = targetPosition + velocity * deltaTime;

        playerComponent.Transform.position = Vector3.Lerp(playerComponent.Transform.position, predictedPosition,
                                        interpolationSpeed * Time.deltaTime);
    }

    // Getter methods for accessing network state
    public bool HasTargetPosition => hasTargetPosition;
    public Vector3 TargetPosition => targetPosition;
    public Vector3 PreviousPosition => previousPosition;
}