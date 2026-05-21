using UnityEngine;

/// <summary>아이소메트릭 시점으로 타겟을 부드럽게 추적하는 카메라</summary>
public class CameraFollow : MonoBehaviour
{
    #region 인스펙터
    [Header("타겟")]
    public Transform target;

    [Header("설정")]
    public Vector3 offset      = new Vector3(0f, 12f, -8f);
    public float   smoothSpeed = 8f;

    [Header("이동 제한 (선택)")]
    public bool    useBounds = false;
    public Vector2 xBounds   = new Vector2(-20f, 20f);
    public Vector2 zBounds   = new Vector2(-20f, 20f);
    #endregion

    #region 내부 변수
    private Vector3 desiredPos;
    #endregion

    #region 생명주기
    void LateUpdate()
    {
        if (target == null) return;

        desiredPos = target.position + offset;

        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, xBounds.x, xBounds.y);
            desiredPos.z = Mathf.Clamp(desiredPos.z, zBounds.x, zBounds.y);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
    #endregion
}
