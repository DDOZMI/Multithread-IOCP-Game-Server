using UnityEngine;

public class PlayerMovementComponent : IPlayerMovement
{
    private readonly IPlayerComponent playerComponent;
    private Vector2 currentMovement;
    private Vector2 lastMoveDirection;
    private bool canMove = true;

    public PlayerMovementComponent(IPlayerComponent playerComponent)
    {
        this.playerComponent = playerComponent;
        this.lastMoveDirection = Vector2.down;
    }

    public void SetCurrentMovement(Vector2 movement)
    {
        if (!canMove)
        {
            currentMovement = Vector2.zero;
            return;
        }

        currentMovement = movement;

        if (movement != Vector2.zero)
        {
            lastMoveDirection = movement;

            // Command Pattern을 사용하지 않는 경우에만 스프라이트 방향 업데이트
            PlayerInputHandler inputHandler = playerComponent.Transform.GetComponent<PlayerInputHandler>();
            if (inputHandler == null || !inputHandler.enableCommandPattern)
            {
                UpdateSpriteDirection(movement);
            }
        }
    }

    public void SetCanMove(bool canMove)
    {
        if (!playerComponent.IsLocalPlayer) return;

        this.canMove = canMove;
        if (!canMove)
        {
            currentMovement = Vector2.zero;
            if (playerComponent.Rigidbody != null)
            {
                playerComponent.Rigidbody.linearVelocity = Vector2.zero;
            }
        }
    }

    public Vector2 GetFacingDirection()
    {
        return lastMoveDirection;
    }

    public void HandleMovement()
    {
        if (!playerComponent.IsLocalPlayer) return;

        PlayerInputHandler inputHandler = playerComponent.Transform.GetComponent<PlayerInputHandler>();

        // Command Pattern을 사용하지 않는 경우에만 Rigidbody로 이동
        if (inputHandler == null || !inputHandler.enableCommandPattern)
        {
            if (playerComponent.Rigidbody != null)
            {
                playerComponent.Rigidbody.linearVelocity = currentMovement * playerComponent.MoveSpeed;
            }
        }
        else
        {
            // Command Pattern 사용 시에는 Transform으로 이미 이동 처리됨
            if (playerComponent.Rigidbody != null)
            {
                playerComponent.Rigidbody.linearVelocity = Vector2.zero;
            }
        }
    }

    public void UpdateSpriteDirection(Vector2 direction)
    {
        if (playerComponent.SpriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
        {
            playerComponent.SpriteRenderer.flipX = direction.x < 0;
        }
    }

    public Vector2 CurrentMovement => currentMovement;
    public Vector2 LastMoveDirection => lastMoveDirection;
}