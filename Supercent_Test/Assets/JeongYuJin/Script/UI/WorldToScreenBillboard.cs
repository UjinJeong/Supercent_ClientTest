using UnityEngine;

/// <summary>
/// 월드 좌표를 스크린 위치로 변환하여 UI 요소를 따라붙게 하는 빌보드 컴포넌트
/// 팝업 텍스트처럼 특정 월드 위치에 UI를 띄울 때 사용
/// </summary>
public class WorldToScreenBillboard : MonoBehaviour
{
    // ── 인스펙터 ─────────────────────────────────────
    public Vector3 worldPosition;   // UI를 표시할 월드 좌표
    public float   lifetime = 1.5f; // 자동 소멸까지의 시간(초)

    // ── 내부 변수 ────────────────────────────────────
    private RectTransform rectTransform;
    private float timer = 0f;

    // ── 유니티 생명주기 ──────────────────────────────
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        // 월드 좌표 → 스크린 좌표 변환 후 RectTransform 위치에 적용
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        rectTransform.position = screenPos;

        // 항상 카메라를 향하도록 회전 (Canvas 렌더 모드에 따라 효과가 다를 수 있음)
        transform.rotation = Camera.main.transform.rotation;

        // 수명이 다하면 오브젝트 소멸
        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
