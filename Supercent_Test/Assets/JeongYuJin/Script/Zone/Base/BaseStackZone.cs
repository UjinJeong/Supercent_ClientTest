using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스택 아이템을 outputPoint에 쌓는 존의 추상 기반 클래스
/// ────────────────────────────────────────────────────
/// [역할] 아이템 스폰 / 소비 / 전체 제거 공통 로직 제공
/// [상속] BaseZone → BaseStackZone → HandcuffZone / HandcuffMoneyZone
/// [확장] 새로운 스택 존 추가 시 ItemPrefab / OutputPoint / StackSpacing 프로퍼티 구현 +
///        ZoneRoutine() 구현만 하면 됨
/// </summary>
public abstract class BaseStackZone : BaseZone
{
    #region 내부 변수
    protected List<GameObject> spawnedItems  = new List<GameObject>(); // 현재 스폰된 아이템 목록 (인덱스 = 스택 높이)
    protected float            cachedSpacing;                           // Awake에서 1회 계산한 스택 간격 캐시
    #endregion

    #region 추상 프로퍼티 (상속 클래스에서 인스펙터 필드 연결)
    /// <summary>스폰할 아이템 프리팹 — 상속 클래스의 인스펙터 필드를 반환</summary>
    protected abstract GameObject ItemPrefab   { get; }
    /// <summary>스택 기준점 Transform — 이 위치 바로 위부터 아이템이 쌓임</summary>
    protected abstract Transform  OutputPoint  { get; }
    /// <summary>인스펙터에서 설정한 기본 스택 간격 — StackUtils가 실제 높이로 보정</summary>
    protected abstract float      StackSpacing { get; }
    #endregion

    #region 생명주기
    protected virtual void Awake()
    {
        // 프리팹 Renderer 실제 높이와 baseSpacing 중 큰 값을 캐시
        // → Start() 이전에 계산해 ZoneRoutine에서 즉시 사용 가능
        cachedSpacing = StackUtils.CalcSpacing(ItemPrefab, StackSpacing);
    }
    #endregion

    #region 스택 조작
    /// <summary>
    /// 스택 최상단에 아이템 1개 스폰 → spawnedItems 리스트에 등록
    /// 위치: OutputPoint + (cachedSpacing × 현재 아이템 수) 높이
    /// </summary>
    protected GameObject SpawnItem()
    {
        if (ItemPrefab == null || OutputPoint == null) return null; // 필수 참조 누락 시 무시

        // 현재 스택 높이(개수)에 비례해 Y 오프셋 계산
        Vector3    pos = OutputPoint.position + Vector3.up * cachedSpacing * spawnedItems.Count;
        GameObject go  = Instantiate(ItemPrefab, pos, OutputPoint.rotation);
        spawnedItems.Add(go); // 목록에 추가해야 이후 ConsumeLastItem / ClearItems 대상이 됨
        return go;
    }

    /// <summary>
    /// 스택 최상단 아이템 1개 Destroy + 목록에서 제거
    /// 성공(아이템 있음) → true / 실패(비어 있음) → false
    /// </summary>
    protected bool ConsumeLastItem()
    {
        if (spawnedItems.Count == 0) return false; // 스택이 비어 있으면 소비 불가

        int last = spawnedItems.Count - 1;         // 최상단 인덱스
        if (spawnedItems[last] != null)
            Destroy(spawnedItems[last]);            // 씬에서 제거
        spawnedItems.RemoveAt(last);               // 리스트에서도 제거
        return true;
    }

    /// <summary>스택 전체 Destroy + 목록 초기화 — 픽업(PickupHandcuffs) 등 일괄 제거 시 사용</summary>
    protected void ClearItems()
    {
        foreach (var go in spawnedItems)
            if (go != null) Destroy(go); // null 체크: 외부에서 이미 파괴된 경우 방어
        spawnedItems.Clear();            // 리스트 비움 (다음 스폰 카운트는 0부터 재시작)
    }
    #endregion
}
