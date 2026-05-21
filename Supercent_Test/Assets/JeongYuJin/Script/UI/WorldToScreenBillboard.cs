using UnityEngine;

/// <summary>월드 좌표를 스크린 위치로 변환해 UI를 따라붙게 하는 빌보드 컴포넌트</summary>
public class WorldToScreenBillboard : MonoBehaviour
{
    #region 인스펙터
    public Vector3 worldPosition;
    public float   lifetime = 1.5f;
    #endregion

    #region 내부 변수
    private RectTransform rectTransform;
    private float         timer = 0f;
    #endregion

    #region 생명주기
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        rectTransform.position = Camera.main.WorldToScreenPoint(worldPosition);
        transform.rotation     = Camera.main.transform.rotation;

        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }
    #endregion
}
