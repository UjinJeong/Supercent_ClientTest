using UnityEngine;

/// <summary>
/// 스택 프리팹 간격 계산 공통 유틸리티
/// PlayerController / HandcuffZone / HandcuffMoneyZone / PrisonerZone에서 공유
/// </summary>
public static class StackUtils
{
    /// <summary>
    /// 프리팹 Renderer 실제 높이를 반영한 스택 간격 반환
    /// Renderer 높이가 baseSpacing보다 크면 자동 적용, 아니면 baseSpacing 사용
    /// </summary>
    public static float CalcSpacing(GameObject prefab, float baseSpacing)
    {
        float spacing = Mathf.Max(0.01f, baseSpacing);
        if (prefab == null) return spacing;
        var r = prefab.GetComponentInChildren<Renderer>();
        if (r != null && r.bounds.size.y > 0f)
            spacing = Mathf.Max(spacing, r.bounds.size.y * 0.9f);
        return spacing;
    }
}
