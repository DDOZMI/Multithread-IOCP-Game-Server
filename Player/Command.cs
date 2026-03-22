using UnityEngine;
using System.Collections.Generic;
using System;

public interface ICommand
{
    void Execute();
    void Undo();
}

// 2D 이동을 위한 기본 Command 클래스
public abstract class Movement2DCommand : ICommand
{
    protected Transform transform;
    protected PlayerController playerController;
    protected Vector3 previousPosition;
    protected float moveSpeed;
    protected float deltaTime;

    public Movement2DCommand(Transform transform, PlayerController playerController, float deltaTime)
    {
        this.transform = transform;
        this.playerController = playerController;
        this.moveSpeed = playerController.moveSpeed;
        this.deltaTime = deltaTime;
    }

    protected abstract Vector2 GetMovementDirection();

    public virtual void Execute()
    {
        if (!playerController.isLocalPlayer) return;

        previousPosition = transform.position;
        Vector2 movement = GetMovementDirection() * moveSpeed * deltaTime;
        transform.Translate(movement, Space.World);

        // 스프라이트 방향 처리
        UpdateSpriteDirection();
    }

    public virtual void Undo()
    {
        if (!playerController.isLocalPlayer) return;
        transform.position = previousPosition;
    }

    protected virtual void UpdateSpriteDirection()
    {
        Vector2 direction = GetMovementDirection();
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            SpriteRenderer spriteRenderer = playerController.GetSpriteRenderer();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
    }
}

// 방향별 이동 Commands
public class MoveUpCommand : Movement2DCommand
{
    public MoveUpCommand(Transform transform, PlayerController playerController, float deltaTime)
        : base(transform, playerController, deltaTime) { }
    protected override Vector2 GetMovementDirection() => Vector2.up;
}

public class MoveDownCommand : Movement2DCommand
{
    public MoveDownCommand(Transform transform, PlayerController playerController, float deltaTime)
        : base(transform, playerController, deltaTime) { }
    protected override Vector2 GetMovementDirection() => Vector2.down;
}

public class MoveLeftCommand : Movement2DCommand
{
    public MoveLeftCommand(Transform transform, PlayerController playerController, float deltaTime)
        : base(transform, playerController, deltaTime) { }
    protected override Vector2 GetMovementDirection() => Vector2.left;
}

public class MoveRightCommand : Movement2DCommand
{
    public MoveRightCommand(Transform transform, PlayerController playerController, float deltaTime)
        : base(transform, playerController, deltaTime) { }
    protected override Vector2 GetMovementDirection() => Vector2.right;
}

public class MoveCommand : Movement2DCommand
{
    private Vector2 direction;

    public MoveCommand(Transform transform, PlayerController playerController, Vector2 direction, float deltaTime)
        : base(transform, playerController, deltaTime)
    {
        this.direction = direction.normalized;
    }

    protected override Vector2 GetMovementDirection() => direction;
}

public class CommandFactory
{
    private Transform transform;
    private PlayerController playerController;

    public CommandFactory(Transform transform, PlayerController playerController)
    {
        this.transform = transform;
        this.playerController = playerController;
    }

    public ICommand CreateMoveCommand(Vector2 direction, float deltaTime)
    {
        if (direction == Vector2.zero) return null;

        // 정확한 방향일 때는 지정된 Command
        if (direction == Vector2.up) return new MoveUpCommand(transform, playerController, deltaTime);
        if (direction == Vector2.down) return new MoveDownCommand(transform, playerController, deltaTime);
        if (direction == Vector2.left) return new MoveLeftCommand(transform, playerController, deltaTime);
        if (direction == Vector2.right) return new MoveRightCommand(transform, playerController, deltaTime);

        // 대각선이나 기타 방향은 일반 MoveCommand
        return new MoveCommand(transform, playerController, direction, deltaTime);
    }
}