using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // 따라갈 대상 Transform
    [Header("Target")]
    public Transform target;

    // 아이소메트릭 카메라 설정: 타겟으로부터의 오프셋과 부드러운 이동 속도
    [Header("Isometric Settings")]
    public Vector3 offset = new Vector3(0f, 12f, -8f);
    public float smoothSpeed = 8f;      

    // 카메라 이동 범위(옵션)
    [Header("Bounds (optional)")]
    public bool useBounds = false;
    // X 축 제한값 (min, max)
    public Vector2 xBounds = new Vector2(-20f, 20f);
    // Z 축 제한값 (min, max)
    public Vector2 zBounds = new Vector2(-20f, 20f);

    // 목표 위치 계산용 변수
    private Vector3 desiredPos;

    void LateUpdate()
    {
        // 대상이 없으면 동작을 멈춤
        if (target == null) return;

        // 타겟 위치에 오프셋을 더해 원하는 위치 계산
        desiredPos = target.position + offset;

        // 범위 사용 시 X,Z 축을 클램프하여 제한
        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, xBounds.x, xBounds.y);
            desiredPos.z = Mathf.Clamp(desiredPos.z, zBounds.x, zBounds.y);
        }

        // 현재 위치에서 원하는 위치로 부드럽게 보간하여 이동
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
