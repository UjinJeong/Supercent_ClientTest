using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 가상 조이스틱 입력 처리
/// [부착 위치] UI Canvas > Joystick (Image + Raycast Target 필수)
/// </summary>
public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    #region 인스펙터
    [Header("참조")]
    public RectTransform background;
    public RectTransform handle;

    [Header("설정")]
    public float handleRange = 1f;  // 핸들 최대 이동 범위
    public float deadZone    = 0f;  // 이 값 이하의 입력은 0 처리
    #endregion

    #region 프로퍼티
    public float Horizontal { get; private set; }
    public float Vertical   { get; private set; }
    #endregion

    #region 내부 변수
    private Camera cam;
    #endregion

    #region 생명주기
    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        cam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera
            ? canvas.worldCamera : null;
    }
    #endregion

    #region 포인터 이벤트
    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, cam, out Vector2 pos)) return;

        // 배경 크기 기준 정규화
        pos.x /= background.sizeDelta.x;
        pos.y /= background.sizeDelta.y;

        // 피벗 보정 후 방향 계산
        float x = (background.pivot.x == 1f) ? pos.x * 2 + 1 : pos.x * 2 - 1;
        float y = (background.pivot.y == 1f) ? pos.y * 2 + 1 : pos.y * 2 - 1;

        Vector2 dir = new Vector2(x, y);
        if (dir.magnitude > 1f) dir.Normalize();

        handle.anchoredPosition = new Vector2(
            dir.x * background.sizeDelta.x / 2f * handleRange,
            dir.y * background.sizeDelta.y / 2f * handleRange);

        if (dir.magnitude < deadZone) dir = Vector2.zero;
        Horizontal = dir.x;
        Vertical   = dir.y;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Horizontal = 0f;
        Vertical   = 0f;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
    #endregion
}
