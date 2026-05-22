using UnityEngine;

/// <summary>
/// 스택 프리팹 간격 계산 공통 유틸리티
/// ──────────────────────────────────
/// [사용처] BaseStackZone.Awake() / PrisonerZone.Start()
/// [목적]  동일한 CalcSpacing 로직을 여러 클래스에서 중복 구현하지 않도록 단일 출처로 제공
/// </summary>
public static class StackUtils
{
    /// <summary>
    /// 프리팹의 Renderer 실제 높이를 반영해 스택 간격을 결정
    /// ──────────────────────────────────────────────────────
    /// • prefab == null    → baseSpacing 그대로 반환
    /// • Renderer 높이 > baseSpacing → Renderer 높이 × 0.9 사용 (살짝 겹쳐 자연스러운 스택)
    /// • Renderer 높이 ≤ baseSpacing → baseSpacing 사용
    /// • 최솟값 0.01f 보장 (0 이하 입력 방어)
    /// </summary>
    public static float CalcSpacing(GameObject prefab, float baseSpacing)
    {
        float spacing = Mathf.Max(0.01f, baseSpacing); // 음수·0 입력 방어

        if (prefab == null) return spacing; // 프리팹 없으면 기본값 반환

        // 첫 번째 Renderer 높이(bounds.size.y)로 실제 크기를 확인
        var r = prefab.GetComponentInChildren<Renderer>();
        if (r != null && r.bounds.size.y > 0f)
            spacing = Mathf.Max(spacing, r.bounds.size.y * 0.9f); // 90%만 사용해 살짝 겹침

        return spacing;
    }
}
