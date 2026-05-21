using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float handleRange = 1f;        // 핸들이 움직일 수 있는 최대 범위 (정규화)
    public float deadZone = 0f;           // 데드존 (이 이하는 0 처리)

    [Header("References")]
    public RectTransform background;      // 조이스틱 배경 원
    public RectTransform handle;          // 조이스틱 핸들

    public float Horizontal { get; private set; }
    public float Vertical   { get; private set; }

    private RectTransform baseRect;
    private Canvas canvas;
    private Camera cam;

    private Vector2 fixedPosition;

    void Start()
    {
        baseRect = GetComponent<RectTransform>();
        canvas   = GetComponentInParent<Canvas>();
        cam      = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;

        fixedPosition = RectTransformUtility.WorldToScreenPoint(cam, background.position);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, cam, out Vector2 pos)) return;

        pos.x /= background.sizeDelta.x;
        pos.y /= background.sizeDelta.y;

        float x = (background.pivot.x == 1f) ? pos.x * 2 + 1 : pos.x * 2 - 1;
        float y = (background.pivot.y == 1f) ? pos.y * 2 + 1 : pos.y * 2 - 1;

        Vector2 inputDir = new Vector2(x, y);
        inputDir = inputDir.magnitude > 1f ? inputDir.normalized : inputDir;

        handle.anchoredPosition = new Vector2(
            inputDir.x * background.sizeDelta.x / 2 * handleRange,
            inputDir.y * background.sizeDelta.y / 2 * handleRange);

        Horizontal = inputDir.x;
        Vertical   = inputDir.y;

        if (inputDir.magnitude < deadZone)
        {
            Horizontal = 0f;
            Vertical   = 0f;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Horizontal = 0f;
        Vertical   = 0f;
        handle.anchoredPosition = Vector2.zero;
    }
}
