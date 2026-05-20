using UnityEngine;

// 교환 존에 쌓이는 수갑 시각 오브젝트 — 개별 픽업 없음, 존이 일괄 수집 관리
public class HandcuffItem : MonoBehaviour
{
    public float autoDestroyTime = 60f;

    void Start()
    {
        Destroy(gameObject, autoDestroyTime);
    }
}
