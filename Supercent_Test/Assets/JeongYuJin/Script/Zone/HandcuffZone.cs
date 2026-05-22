using System.Collections;
using UnityEngine;

/// <summary>
/// 수갑 교환 존 (BaseStackZone 상속)
/// ────────────────────────────────
/// [진입] 플레이어 보유 돌 1개씩 소비 → outputPoint에 수갑 세로 스폰
/// [픽업] 플레이어가 outputPoint 근처 접근 시 수갑 전량 자동 획득
/// [흐름] ZoneRoutine(돌→수갑 변환) ↔ Update(픽업 감지) 가 독립 실행
/// </summary>
public class HandcuffZone : BaseStackZone
{
    #region 인스펙터
    [Header("참조")]
    public Transform  outputPoint;     // 수갑이 쌓일 기준 위치
    public GameObject handcuffPrefab;  // 스폰할 수갑 프리팹

    [Header("사운드")]
    public AudioClip spawnSFX;         // 수갑 스폰 시 재생할 효과음
    public float     spawnSFXVolume = 1f;

    [Header("설정")]
    public float exchangeInterval = 0.3f; // 돌 1개 → 수갑 1개 변환 주기(초) ← 조절 포인트
    public float stackSpacing     = 0.4f; // 수갑 간 세로 간격 ← 조절 포인트
    public float pickupDistance   = 1.5f; // 픽업 인식 거리 ← 조절 포인트
    #endregion

    #region BaseStackZone 추상 프로퍼티 구현
    // 인스펙터 필드명을 유지하면서 베이스 클래스 추상 프로퍼티에 연결
    protected override GameObject ItemPrefab   => handcuffPrefab;
    protected override Transform  OutputPoint  => outputPoint;
    protected override float      StackSpacing => stackSpacing;
    #endregion

    #region 생명주기
    void Update()
    {
        // 스택이 비어 있거나 기준점이 없으면 픽업 검사 불필요
        if (spawnedItems.Count == 0 || outputPoint == null) return;

        // 트리거와 무관하게 outputPoint 주변을 매 프레임 탐색 → 수갑 픽업
        // (Trigger 방식 대신 OverlapSphere 사용: 여러 수갑이 쌓여도 중심점 하나로 감지)
        Collider[] hits = Physics.OverlapSphere(outputPoint.position, pickupDistance);
        foreach (var hit in hits)
        {
            PlayerController p = hit.GetComponent<PlayerController>();
            if (p == null) continue; // PlayerController가 없는 오브젝트는 무시

            PickupHandcuffs(p); // 수갑 전량 플레이어에게 이전
            return;             // 픽업 처리 후 즉시 종료 (중복 처리 방지)
        }
    }
    #endregion

    #region 교환 루틴 (BaseZone 구현)
    protected override IEnumerator ZoneRoutine()
    {
        while (true)
        {
            // 플레이어가 존재하지 않거나 돌이 없으면 다음 프레임으로 넘김
            if (player == null || player.CarriedMoney <= 0) { yield return null; continue; }

            // 돌 1개 소비 성공 시 수갑 1개 스폰
            if (player.ConsumeRock(1))
            {
                GameObject go = SpawnItem(); // BaseStackZone: 최상단에 수갑 1개 추가
                if (go != null && spawnSFX != null)
                    AudioSource.PlayClipAtPoint(spawnSFX, outputPoint.position, spawnSFXVolume);
            }

            yield return new WaitForSeconds(exchangeInterval); // 다음 변환까지 대기
        }
    }
    #endregion

    #region 픽업
    /// <summary>
    /// outputPoint 근처 플레이어에게 수갑 전량 이전 + 스택 초기화 + 코루틴 중지
    /// ClearItems() → Destroy만 하고 재스폰은 하지 않음
    /// StopZoneRoutine() → player 참조 해제 + 코루틴 정지 (ZoneRoutine 재진입은 OnTriggerEnter가 담당)
    /// </summary>
    void PickupHandcuffs(PlayerController p)
    {
        p.AddHandcuffToCarry(spawnedItems.Count); // 현재 스택 수만큼 플레이어 인벤토리에 추가
        ClearItems();      // BaseStackZone: 씬의 수갑 오브젝트 전부 Destroy + 목록 초기화
        StopZoneRoutine(); // BaseZone: ZoneRoutine 코루틴 중단 + player = null
    }
    #endregion
}
