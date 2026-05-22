using System.Collections;
using UnityEngine;

/// <summary>
/// 수갑 교환 존 (BaseStackZone 상속)
/// [진입] 보유 돌 1개씩 소비 → outputPoint에 수갑 세로 스폰
/// [픽업] 플레이어가 outputPoint 근처 접근 시 수갑 전량 획득
/// </summary>
public class HandcuffZone : BaseStackZone
{
    #region 인스펙터
    [Header("참조")]
    public Transform  outputPoint;
    public GameObject handcuffPrefab;

    [Header("사운드")]
    public AudioClip spawnSFX;
    public float     spawnSFXVolume = 1f;

    [Header("설정")]
    public float exchangeInterval = 0.3f; // 돌 1개 → 수갑 1개 변환 주기(초)
    public float stackSpacing     = 0.4f; // 수갑 간 세로 간격
    public float pickupDistance   = 1.5f; // 픽업 인식 거리
    #endregion

    #region BaseStackZone 추상 프로퍼티 구현
    // 인스펙터 필드명을 유지하면서 베이스 클래스에 연결
    protected override GameObject ItemPrefab   => handcuffPrefab;
    protected override Transform  OutputPoint  => outputPoint;
    protected override float      StackSpacing => stackSpacing;
    #endregion

    #region 생명주기
    void Update()
    {
        if (spawnedItems.Count == 0 || outputPoint == null) return;

        // 트리거와 무관하게 outputPoint 주변 탐색 → 픽업
        Collider[] hits = Physics.OverlapSphere(outputPoint.position, pickupDistance);
        foreach (var hit in hits)
        {
            PlayerController p = hit.GetComponent<PlayerController>();
            if (p == null) continue;
            PickupHandcuffs(p);
            return;
        }
    }
    #endregion

    #region 교환 루틴 (BaseZone 구현)
    protected override IEnumerator ZoneRoutine()
    {
        while (true)
        {
            if (player == null || player.CarriedMoney <= 0) { yield return null; continue; }

            if (player.ConsumeRock(1))
            {
                GameObject go = SpawnItem(); // BaseStackZone.SpawnItem()
                if (go != null && spawnSFX != null)
                    AudioSource.PlayClipAtPoint(spawnSFX, outputPoint.position, spawnSFXVolume);
            }

            yield return new WaitForSeconds(exchangeInterval);
        }
    }
    #endregion

    #region 픽업
    void PickupHandcuffs(PlayerController p)
    {
        p.AddHandcuffToCarry(spawnedItems.Count);
        ClearItems();        // BaseStackZone.ClearItems()
        StopZoneRoutine();   // BaseZone.StopZoneRoutine()
    }
    #endregion
}
