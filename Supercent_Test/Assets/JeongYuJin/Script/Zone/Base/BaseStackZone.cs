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
    protected List<GameObject> spawnedItems  = new List<GameObject>();
    protected float            cachedSpacing;
    #endregion

    #region 추상 프로퍼티 (상속 클래스에서 인스펙터 필드 연결)
    /// <summary>스폰할 아이템 프리팹</summary>
    protected abstract GameObject ItemPrefab   { get; }
    /// <summary>스택 기준점 Transform</summary>
    protected abstract Transform  OutputPoint  { get; }
    /// <summary>인스펙터에서 설정한 기본 스택 간격</summary>
    protected abstract float      StackSpacing { get; }
    #endregion

    #region 생명주기
    protected virtual void Awake()
    {
        // 프리팹 실제 높이 기반 간격 캐시 (StackUtils 공통 유틸 사용)
        cachedSpacing = StackUtils.CalcSpacing(ItemPrefab, StackSpacing);
    }
    #endregion

    #region 스택 조작
    /// <summary>스택 최상단에 아이템 1개 스폰 → spawnedItems에 등록</summary>
    protected GameObject SpawnItem()
    {
        if (ItemPrefab == null || OutputPoint == null) return null;
        Vector3    pos = OutputPoint.position + Vector3.up * cachedSpacing * spawnedItems.Count;
        GameObject go  = Instantiate(ItemPrefab, pos, OutputPoint.rotation);
        spawnedItems.Add(go);
        return go;
    }

    /// <summary>스택 최상단 아이템 1개 제거 — 성공 시 true</summary>
    protected bool ConsumeLastItem()
    {
        if (spawnedItems.Count == 0) return false;
        int last = spawnedItems.Count - 1;
        if (spawnedItems[last] != null) Destroy(spawnedItems[last]);
        spawnedItems.RemoveAt(last);
        return true;
    }

    /// <summary>스택 전체 제거</summary>
    protected void ClearItems()
    {
        foreach (var go in spawnedItems)
            if (go != null) Destroy(go);
        spawnedItems.Clear();
    }
    #endregion
}
