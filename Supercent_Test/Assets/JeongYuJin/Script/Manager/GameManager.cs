using UnityEngine;

/// <summary>
/// 게임 전반의 데이터(돈)를 관리하는 싱글톤 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── 싱글톤 ───────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── 인스펙터 ─────────────────────────────────────
    [Header("게임 데이터")]
    public int money = 0;

    // ── 이벤트 ───────────────────────────────────────
    /// <summary>돈이 변경될 때 발생 — 인자: 변경 후 금액</summary>
    public System.Action<int> OnMoneyChanged;

    // ── 유니티 생명주기 ──────────────────────────────
    void Awake()
    {
        // 씬에 인스턴스가 이미 존재하면 자신을 제거 (싱글톤 보장)
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 공개 메서드 ──────────────────────────────────
    /// <summary>돈을 추가하고 변경 이벤트를 발행한다</summary>
    public void AddMoney(int amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke(money);
    }

    /// <summary>
    /// 돈을 차감한다.
    /// </summary>
    /// <returns>잔액이 충분하면 true, 부족하면 false</returns>
    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }
}
