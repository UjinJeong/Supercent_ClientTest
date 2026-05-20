using UnityEngine;

// 월드 좌표를 스크린 위치로 매 프레임 업데이트하고 카메라를 바라보게 하는 컴포넌트
// 팝업(UI 요소)이 항상 카메라 정면을 향하도록 유지하며 지정된 시간이 지나면 스스로 파괴한다.
public class WorldToScreenBillboard : MonoBehaviour
{
    // 팝업이 따라갈 월드 좌표
    public Vector3 worldPosition;
    // 팝업 생존 시간(초)
    public float lifetime = 1.5f;

    private RectTransform rectTransform;
    private float timer = 0f;

    void Awake()
    {
        // RectTransform 캐시
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        // 월드 좌표를 스크린 좌표로 변환하여 RectTransform 위치에 적용
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        rectTransform.position = screenPos;

        // 팝업이 화면 상에서 카메라를 바라보도록 회전을 카메라와 동일하게 설정
        // (Canvas Render Mode에 따라 회전 효과가 다를 수 있음)
        transform.rotation = Camera.main.transform.rotation;

        // 생존 시간 카운트 및 만료 시 파괴
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}