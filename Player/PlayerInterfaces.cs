using UnityEngine;

public interface IPlayerMovement
{
    Vector2 CurrentMovement { get; }
    Vector2 LastMoveDirection { get; }

    void SetCurrentMovement(Vector2 movement);
    void SetCanMove(bool canMove);
    Vector2 GetFacingDirection();
    void HandleMovement();
    void UpdateSpriteDirection(Vector2 direction);
}

public interface IPlayerNetwork
{
    bool HasTargetPosition { get; }
    Vector3 TargetPosition { get; }

    void CheckAndSendPosition();
    void SetNetworkPosition(float x, float y);
    void SetPosition(float x, float y);
    void HandleRemotePlayerMovement();
    void PredictiveMove(float deltaTime);
}

public interface IPlayerAnimation
{
    void UpdateAnimation();
    void SetAnimationSpeed(float speed);
}

public interface IPlayerComponent
{
    Transform Transform { get; }
    bool IsLocalPlayer { get; }
    float MoveSpeed { get; }
    Vector2 CurrentMovement { get; }
    Vector2 LastMoveDirection { get; }
    Rigidbody2D Rigidbody { get; }
    Animator Animator { get; }
    SpriteRenderer SpriteRenderer { get; }
}