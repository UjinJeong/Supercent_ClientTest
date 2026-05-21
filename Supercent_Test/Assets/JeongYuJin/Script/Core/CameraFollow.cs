using UnityEngine;

/// <summary>
/// 아이소메트릭 시점으로 타겟을 부드럽게 추적하는 카메라
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // ── 인스펙터 ─────────────────────────────────────
    [Header("타겟")]
    public Transform target;                                // 추적할 대상 (플레이어)

    [Header("아이소메트릭 설정")]
    public Vector3 offset      = new Vector3(0f, 12f, -8f); // 타겟으로부터의 카메라 오프셋
    public float   smoothSpeed = 8f;                        // 추적 보간 속도

    [Header("이동 제한 (선택)")]
    public bool    useBounds = false;                       // 카메라 이동 범위 제한 사용 여부
    public Vector2 xBounds   = new Vector2(-20f, 20f);     // X축 이동 가능 범위 (min, max)
    public Vector2 zBounds   = new Vector2(-20f, 20f);     // Z축 이동 가능 범위 (min, max)

    // ── 내부 변수 ────────────────────────────────────
    private Vector3 desiredPos; // 매 프레임 목표 위치

    // ── 유니티 생명주기 ──────────────────────────────
    void LateUpdate()
    {
        if (target == null) return;

        // 타겟 위치에 오프셋을 더해 목표 위치 계산
        desiredPos = target.position + offset;

        // 이동 범위 제한 적용 (useBounds가 true일 때만)
        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, xBounds.x, xBounds.y);
            desiredPos.z = Mathf.Clamp(desiredPos.z, zBounds.x, zBounds.y);
        }

        // 현재 위치에서 목표 위치로 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
