using UnityEngine;

/// <summary>게임 전반의 데이터(돈)를 관리하는 싱글톤 매니저</summary>
public class GameManager : MonoBehaviour
{
    #region 싱글톤
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }
    #endregion

    #region 인스펙터
    [Header("게임 데이터")]
    public int money = 0;
    #endregion

    #region 이벤트
    /// <summary>돈이 변경될 때 발생 — 인자: 변경 후 금액</summary>
    public System.Action<int> OnMoneyChanged;
    #endregion

    #region 공개 메서드
    public void AddMoney(int amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke(money);
    }

    /// <returns>잔액 부족 시 false</returns>
    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }
    #endregion
}
