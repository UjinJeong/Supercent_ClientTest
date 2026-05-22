using System.Collections;
using UnityEngine;

/// <summary>
/// 수갑 테이블 존 (BaseStackZone 상속)
/// [진입] 플레이어 수갑 1개씩 소비 → outputPoint에 수갑 스폰
/// [수감자] PrisonerZone이 HandcuffCount / TryConsumeTableHandcuff()로 수갑 소비
///          → 돈 지급은 PrisonerZone(수감자 처리 완료 시)이 담당
/// </summary>
public class HandcuffMoneyZone : BaseStackZone
{
    #region 인스펙터
    [Header("참조")]
    public Transform  outputPoint;
    public GameObject handcuffPrefab;

    [Header("사운드")]
    public AudioClip depositSFX;
    public float     depositSFXVolume = 1f;

    [Header("설정")]
    public float depositInterval = 0.3f;  // 수갑 1개 처리 주기(초)
    public float stackSpacing    = 0.15f; // 쌓인 수갑 간 세로 간격
    #endregion

    #region BaseStackZone 추상 프로퍼티 구현
    protected override GameObject ItemPrefab   => handcuffPrefab;
    protected override Transform  OutputPoint  => outputPoint;
    protected override float      StackSpacing => stackSpacing;
    #endregion

    #region 공개 프로퍼티 / 메서드 (PrisonerZone용)
    /// <summary>현재 테이블 위 수갑 수</summary>
    public int HandcuffCount => spawnedItems.Count;

    /// <summary>테이블에서 수갑 1개 제거 — 성공 시 true</summary>
    public bool TryConsumeTableHandcuff() => ConsumeLastItem(); // BaseStackZone
    #endregion

    #region 입금 루틴 (BaseZone 구현)
    protected override IEnumerator ZoneRoutine()
    {
        while (true)
        {
            if (player == null || player.CarriedHandcuffs <= 0) { yield return null; continue; }

            // 수갑 1개 소비 → 책상에 스폰 (돈 지급은 PrisonerZone이 담당)
            if (player.ConsumeHandcuff(1))
            {
                SpawnItem(); // BaseStackZone.SpawnItem()
                if (depositSFX != null)
                    AudioSource.PlayClipAtPoint(depositSFX, transform.position, depositSFXVolume);
            }

            yield return new WaitForSeconds(depositInterval);
        }
    }
    #endregion
}
