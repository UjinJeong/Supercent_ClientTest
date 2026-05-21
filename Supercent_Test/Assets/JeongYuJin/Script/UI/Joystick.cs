using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 가상 조이스틱 입력 처리
/// [부착 위치] UI Canvas > Joystick 오브젝트 (Image + Raycast Target 필수)
/// </summary>
public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // ── 인스펙터 ─────────────────────────────────────
    [Header("참조")]
    public RectTransform background;    // 조이스틱 배경 원 RectTransform
    public RectTransform handle;        // 조이스틱 핸들 RectTransform

    [Header("설정")]
    public float handleRange = 1f;      // 핸들 최대 이동 범위 (1 = 배경 반지름 100%)
    public float deadZone    = 0f;      // 이 값 이하의 입력은 0으로 처리

    // ── 공개 프로퍼티 ────────────────────────────────
    /// <summary>현재 수평 입력값 (-1 ~ 1)</summary>
    public float Horizontal { get; private set; }
    /// <summary>현재 수직 입력값 (-1 ~ 1)</summary>
    public float Vertical   { get; private set; }

    // ── 내부 변수 ────────────────────────────────────
    private Camera cam; // Screen Space - Camera 모드일 때 사용하는 카메라

    // ── 유니티 생명주기 ──────────────────────────────
    void Start()
    {
        // 캔버스 렌더 모드에 따라 카메라 참조 결정
        Canvas canvas = GetComponentInParent<Canvas>();
        cam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera
            ? canvas.worldCamera
            : null;
    }

    // ── 포인터 이벤트 ────────────────────────────────
    /// <summary>손가락/마우스를 누르는 순간 드래그 처리를 바로 실행</summary>
    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    /// <summary>드래그 중 핸들 위치와 입력값을 갱신</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        // 터치 위치를 배경 RectTransform의 로컬 좌표로 변환
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, cam, out Vector2 pos)) return;

        // 배경 크기 기준으로 -1 ~ 1 정규화
        pos.x /= background.sizeDelta.x;
        pos.y /= background.sizeDelta.y;

        // 피벗 위치에 따라 방향값 보정
        float x = (background.pivot.x == 1f) ? pos.x * 2 + 1 : pos.x * 2 - 1;
        float y = (background.pivot.y == 1f) ? pos.y * 2 + 1 : pos.y * 2 - 1;

        // 크기가 1을 넘으면 정규화 (원 밖으로 나가지 않도록)
        Vector2 dir = new Vector2(x, y);
        if (dir.magnitude > 1f) dir.Normalize();

        // 핸들 위치 갱신
        handle.anchoredPosition = new Vector2(
            dir.x * background.sizeDelta.x / 2f * handleRange,
            dir.y * background.sizeDelta.y / 2f * handleRange);

        // 데드존 이하면 입력 0 처리
        if (dir.magnitude < deadZone) dir = Vector2.zero;

        Horizontal = dir.x;
        Vertical   = dir.y;
    }

    /// <summary>손가락/마우스를 떼면 핸들을 원점으로 복귀하고 입력값 초기화</summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        Horizontal = 0f;
        Vertical   = 0f;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
