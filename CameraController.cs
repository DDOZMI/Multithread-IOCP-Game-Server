using UnityEngine;

public class CameraController : MonoBehaviour
{
    // 따라갈 대상 (로컬 플레이어)
    private Transform target;

    [Header("Camera Settings")]
    [Tooltip("카메라가 플레이어를 얼마나 부드럽게 따라갈지 설정합니다.")]
    public float smoothSpeed = 10f;

    [Tooltip("플레이어로부터 카메라가 얼마나 떨어져 있을지 설정합니다.")]
    public Vector3 offset = new Vector3(0, 0, -10); // 2D게임이면 Z값 음수 설정

    void LateUpdate()
    {
        // target이 없으면 로컬 플레이어 찾기
        if (target == null)
        {
            // Scene에 있는 모든 PlayerController 찾기
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (PlayerController player in players)
            {
                // 로컬 플레이어 찾기
                if (player.isLocalPlayer)
                {
                    target = player.transform;
                    Debug.Log("[CameraController] 카메라가 로컬 플레이어를 찾았습니다: " + target.name);
                    break;
                }
            }

            if (target == null)
            {
                return;
            }
        }

        // 대상을 찾으면 카메라 위치를 부드럽게 이동
        Vector3 desiredPosition = target.position + offset;

        // 목표 위치로 보간
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 카메라 위치 업데이트
        transform.position = smoothedPosition;
    }
}