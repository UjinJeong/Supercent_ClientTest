using System.Collections;
using UnityEngine;

/// <summary>
/// 수갑 테이블 존 (BaseStackZone 상속)
/// ─────────────────────────────────────────────────────
/// [진입] 플레이어 수갑 1개씩 소비 → outputPoint에 수갑 스폰 (테이블 위에 쌓임)
/// [수감자] PrisonerZone이 HandcuffCount / TryConsumeTableHandcuff()로 테이블 수갑 소비
///          → 돈 지급은 PrisonerZone(수감자 처리 완료 시)이 담당
/// </summary>
public class HandcuffMoneyZone : BaseStackZone
{
    #region 인스펙터
    [Header("참조")]
    public Transform  outputPoint;    // 수갑이 쌓일 테이블 위 기준 위치
    public GameObject handcuffPrefab; // 스폰할 수갑 프리팹

    [Header("사운드")]
    public AudioClip depositSFX;              // 수갑 입금 효과음
    public float     depositSFXVolume = 1f;

    [Header("설정")]
    public float depositInterval = 0.3f;  // 수갑 1개 처리 주기(초) ← 조절 포인트
    public float stackSpacing    = 0.15f; // 쌓인 수갑 간 세로 간격 ← 조절 포인트
    #endregion

    #region BaseStackZone 추상 프로퍼티 구현
    // 인스펙터 필드명을 유지하면서 베이스 클래스 추상 프로퍼티에 연결
    protected override GameObject ItemPrefab   => handcuffPrefab;
    protected override Transform  OutputPoint  => outputPoint;
    protected override float      StackSpacing => stackSpacing;
    #endregion

    #region 공개 프로퍼티 / 메서드 (PrisonerZone 전용)
    /// <summary>현재 테이블 위 수갑 수 — PrisonerZone이 스폰 조건·요구량 계산에 사용</summary>
    public int HandcuffCount => spawnedItems.Count;

    /// <summary>
    /// 테이블에서 수갑 1개 제거 — PrisonerZone이 수감자 처리 중 1개씩 호출
    /// 내부적으로 BaseStackZone.ConsumeLastItem() 위임 → 성공 시 true
    /// </summary>
    public bool TryConsumeTableHandcuff() => ConsumeLastItem(); // BaseStackZone
    #endregion

    #region 입금 루틴 (BaseZone 구현)
    protected override IEnumerator ZoneRoutine()
    {
        while (true)
        {
            // 플레이어가 존재하지 않거나 손에 수갑이 없으면 다음 프레임으로 넘김
            if (player == null || player.CarriedHandcuffs <= 0) { yield return null; continue; }

            // 플레이어 수갑 1개 소비 → 테이블에 수갑 스폰
            // (돈 지급은 이 클래스가 아닌 PrisonerZone이 수감자 처리 완료 후 담당)
            if (player.ConsumeHandcuff(1))
            {
                SpawnItem(); // BaseStackZone: 테이블 최상단에 수갑 1개 추가
                if (depositSFX != null)
                    AudioSource.PlayClipAtPoint(depositSFX, transform.position, depositSFXVolume);
            }

            yield return new WaitForSeconds(depositInterval); // 다음 입금까지 대기
        }
    }
    #endregion
}
