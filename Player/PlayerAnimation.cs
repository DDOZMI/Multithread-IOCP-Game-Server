using UnityEngine;

public class PlayerAnimationComponent : IPlayerAnimation
{
    private readonly IPlayerComponent playerComponent;
    private readonly IPlayerNetwork networkComponent;

    // 애니메이터 파라미터 해시값
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public PlayerAnimationComponent(IPlayerComponent playerComponent, IPlayerNetwork networkComponent)
    {
        this.playerComponent = playerComponent;
        this.networkComponent = networkComponent;
    }

    public void UpdateAnimation()
    {
        if (playerComponent.Animator == null) return;

        float speed = CalculateAnimationSpeed();
        SetAnimationSpeed(speed);
    }

    public void SetAnimationSpeed(float speed)
    {
        if (playerComponent.Animator != null)
        {
            playerComponent.Animator.SetFloat(SpeedHash, speed);
        }
    }

    private float CalculateAnimationSpeed()
    {
        float speed;

        if (playerComponent.IsLocalPlayer)
        {
            speed = playerComponent.CurrentMovement.magnitude;
        }
        else
        {
            // 원격 플레이어의 경우 Rigidbody 속도 기반으로 계산
            speed = playerComponent.Rigidbody.linearVelocity.magnitude / playerComponent.MoveSpeed;

            // 네트워크 보간 중인 경우 최소 속도 보장
            if (networkComponent.HasTargetPosition &&
                Vector3.Distance(playerComponent.Transform.position, networkComponent.TargetPosition) > 0.1f)
            {
                speed = Mathf.Max(speed, 0.5f);
            }
        }

        return speed;
    }
}