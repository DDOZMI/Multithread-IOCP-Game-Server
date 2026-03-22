using UnityEngine;
using TMPro;

public class PlayerNameDisplay : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 nameOffset = new Vector3(0, -0.5f, 0);

    private GameObject namePanel;
    private TMP_Text nameText;
    private Transform playerTransform;
    private Canvas worldCanvas;

    public void Initialize(Transform player, string nickname, GameObject namePanelPrefab)
    {
        playerTransform = player;

        // Canvas 찾기
        worldCanvas = FindFirstObjectByType<Canvas>();

        if (worldCanvas == null)
        {
            Debug.LogError("[PlayerNameDisplay] Scene에서 Canvas를 찾을 수 없습니다!");
            return;
        }

        if (namePanelPrefab != null)
        {
            // NamePanel 인스턴스 생성
            namePanel = Instantiate(namePanelPrefab, worldCanvas.transform);
            nameText = namePanel.GetComponent<TMP_Text>();

            if (nameText != null)
            {
                nameText.text = nickname;
                Debug.Log($"[PlayerNameDisplay] 닉네임 '{nickname}' 이 플레이어에 띄워집니다.");
            }
            else
            {
                Debug.LogError("[PlayerNameDisplay] NamePanel prefab에 Text 컴포넌트를 찾을 수 없습니다!");
            }

            UpdatePosition();
        }
        else
        {
            Debug.LogError("[PlayerNameDisplay] NamePanel prefab을 찾을 수 없습니다!");
        }
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (namePanel == null || playerTransform == null || worldCanvas == null) return;

        // 월드 좌표를 스크린 좌표로 변환
        Vector3 worldPosition = playerTransform.position + nameOffset;

        Camera cam = worldCanvas.worldCamera ?? Camera.main;
        if (cam == null) return;

        Vector3 screenPoint = cam.WorldToScreenPoint(worldPosition);

        // Canvas 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            worldCanvas.transform as RectTransform,
            screenPoint,
            cam,
            out Vector2 localPoint);

        if (namePanel != null)
        {
            namePanel.GetComponent<RectTransform>().localPosition = localPoint;
        }
    }

    public void UpdateNickname(string newNickname)
    {
        if (nameText != null)
        {
            nameText.text = newNickname;
            Debug.Log($"[PlayerNameDisplay] 닉네임 없데이트: {newNickname}");
        }
    }

    private void OnDestroy()
    {
        if (namePanel != null)
        {
            Destroy(namePanel);
        }
    }
}