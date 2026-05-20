using UnityEngine;

// 바위가 부서질 때 생성되는 수집 가능한 돌 아이템
public class RockItem : MonoBehaviour
{
    public float autoDestroyTime = 15f;

    void Start()
    {
        Destroy(gameObject, autoDestroyTime);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;
        if (pc.PickupRock(1))
            Destroy(gameObject);
    }
}
