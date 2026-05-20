using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 전역에서 GameManager에 접근할 때 사용
    public static GameManager Instance { get; private set; }

    [Header("Game Data")]
    // 현재 소지 금액
    public int money = 0;
    // 현재 수감자 수
    public int prisonerCount = 0;
    // 허용 가능한 최대 수감자 수
    public int maxPrisoners = 20;
    // 보유 수갑 수량
    public int handcuffCount = 0;

    [Header("Events")]
    // 금액 변경 시 호출된다. 인자로 현재 금액을 전달
    public System.Action<int> OnMoneyChanged;
    // 수감자 수 변경 시 호출된다. 인자로 (현재수감자수, 최대수감자수)를 전달
    public System.Action<int, int> OnPrisonerCountChanged;
    // 수갑 수량 변경 시 호출된다. 인자로 현재 수갑 수량을 전달
    public System.Action<int> OnHandcuffChanged;

    void Awake()
    {
        // 싱글톤 패턴: 이미 인스턴스가 존재하면 중복 제거
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // 씬 전환 시에도 GameObject 유지
        DontDestroyOnLoad(gameObject);
    }

    // 금액을 추가하고 변경 이벤트를 호출
    public void AddMoney(int amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke(money);
    }

    // 금액을 소비하려 시도. 충분하지 않으면 false 반환
    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }

    // 수감자 추가 시도. 최대를 넘으면 false 반환
    public bool AddPrisoner()
    {
        if (prisonerCount >= maxPrisoners) return false;
        prisonerCount++;
        OnPrisonerCountChanged?.Invoke(prisonerCount, maxPrisoners);
        return true;
    }

    // 수감자 제거: 최소값 0으로 유지하고 이벤트 호출
    public void RemovePrisoner()
    {
        prisonerCount = Mathf.Max(0, prisonerCount - 1);
        OnPrisonerCountChanged?.Invoke(prisonerCount, maxPrisoners);
    }

    // 수갑 추가 (기본 1개) 및 이벤트 호출
    public void AddHandcuff(int count = 1)
    {
        handcuffCount += count;
        OnHandcuffChanged?.Invoke(handcuffCount);
    }

    // 수갑 사용 시도. 없으면 false 반환
    public bool UseHandcuff()
    {
        if (handcuffCount <= 0) return false;
        handcuffCount--;
        OnHandcuffChanged?.Invoke(handcuffCount);
        return true;
    }
}