using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 돈 입금 존 (책상 앞에서 돈 제출)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class MoneyDepositZone : MonoBehaviour
{
    [Header("Deposit")]
    // 돈 하나씩 입금되는 속도(초)
    public float depositInterval = 0.1f;

    // 현재 입금 중인 플레이어 참조
    private PlayerController player;
    // 입금 루틴 동작 플래그
    private bool isDepositing = false;

    // 플레이어가 존에 들어오면 입금 시작
    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;
        player = pc;
        if (!isDepositing) StartCoroutine(DepositRoutine());
    }

    // 플레이어가 존을 떠나면 입금 중지
    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            player = null;
            isDepositing = false;
            StopAllCoroutines();
        }
    }

    // 일정 간격으로 플레이어의 돈을 입금 처리하는 코루틴
    IEnumerator DepositRoutine()
    {
        isDepositing = true;
        while (player != null && player.CarriedMoney > 0)
        {
            player.DepositMoney();
            yield return new WaitForSeconds(depositInterval);
        }
        isDepositing = false;
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 수갑 픽업 존 (경찰서 입구)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class HandcuffPickupZone : MonoBehaviour
{
    [Header("Handcuff")]
    // 획득할 수갑 수량
    public int handcuffAmount = 4;
    // 재생성 대기 시간(초)
    public float respawnTime = 10f;
    // 시각적으로 보이는 오브젝트 (활성/비활성으로 표시)
    public GameObject visualObject;

    // 현재 사용 가능 여부
    private bool available = true;

    // 플레이어가 존에 들어오면 수갑 지급 및 비활성화 시작
    void OnTriggerEnter(Collider other)
    {
        if (!available) return;
        if (other.GetComponent<PlayerController>() == null) return;

        GameManager.Instance.AddHandcuff(handcuffAmount);
        available = false;
        if (visualObject) visualObject.SetActive(false);
        StartCoroutine(Respawn());
    }

    // 일정 시간 후 다시 활성화
    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        available = true;
        if (visualObject) visualObject.SetActive(true);
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 죄수 체포 존 (플레이어가 죄수 근처 → 자동 체포)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class ArrestZone : MonoBehaviour
{
    [Header("Cell Positions")]
    // 감방(셀) 위치들
    public Transform[] cellPoints;

    // 순차적으로 할당할 인덱스
    private int cellIndex = 0;

    // 죄수가 존에 들어오면 수감자별 랜덤 수갑 수량만큼 소비 후 체포 처리
    void OnTriggerEnter(Collider other)
    {
        Prisoner prisoner = other.GetComponent<Prisoner>();
        if (prisoner == null || prisoner.state != Prisoner.PrisonerState.Free) return;
        if (GameManager.Instance.handcuffCount < prisoner.requiredHandcuffs) return;
        if (GameManager.Instance.prisonerCount >= GameManager.Instance.maxPrisoners) return;

        for (int i = 0; i < prisoner.requiredHandcuffs; i++)
            GameManager.Instance.UseHandcuff();

        Transform cell = cellPoints[cellIndex % cellPoints.Length];
        cellIndex++;
        prisoner.Arrest(cell);
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 돌 교환 존 (돌 1개씩 소비 → 플레이어 수갑 스택에 직접 추가)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class RockExchangeZone : MonoBehaviour
{
    [Header("Exchange")]
    // 돌 1개당 교환 간격(초)
    public float exchangeInterval = 0.2f;
    // 기계 출력구 위치 (수갑이 잠깐 나타나는 시각 효과용)
    public Transform outputPoint;
    public GameObject handcuffVisualPrefab;

    private PlayerController player;
    private bool isExchanging;

    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;
        player = pc;
        if (!isExchanging && player.CarriedRocks > 0)
            StartCoroutine(ExchangeRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        player = null;
        isExchanging = false;
        StopAllCoroutines();
    }

    // 돌 1개 소비 → 기계에서 수갑 시각 효과 → 플레이어 수갑 스택에 추가
    IEnumerator ExchangeRoutine()
    {
        isExchanging = true;
        while (player != null && player.UseCarriedRock(1))
        {
            ShowHandcuffVisual();
            player.AddHandcuffToCarry(1);
            yield return new WaitForSeconds(exchangeInterval);
        }
        isExchanging = false;
    }

    // 기계 출력구에 수갑 프리팹을 잠깐 생성 (시각 피드백)
    void ShowHandcuffVisual()
    {
        if (handcuffVisualPrefab == null || outputPoint == null) return;
        GameObject go = Instantiate(handcuffVisualPrefab, outputPoint.position, Quaternion.identity);
        Destroy(go, exchangeInterval * 0.8f);
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 수갑 배분 존 (책상 — 플레이어가 수갑을 내려놓으면서
// 근처 자유 수감자에게 필요한 수만큼 지급 → 체포 → 돈 생성)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class HandcuffDistributionZone : MonoBehaviour
{
    [Header("Distribution")]
    // 수갑 하나 배분 간격(초)
    public float distributeInterval = 0.3f;
    // 체포 대상 탐색 반경
    public float searchRadius = 8f;
    // 체포 시 지급 금액
    public int moneyPerArrest = 50;

    [Header("Cell Positions")]
    public Transform[] cellPoints;

    private PlayerController player;
    private bool isDistributing;
    private int cellIndex = 0;

    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;
        player = pc;
        if (!isDistributing) StartCoroutine(DistributeRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        player = null;
        isDistributing = false;
        StopAllCoroutines();
    }

    IEnumerator DistributeRoutine()
    {
        isDistributing = true;
        while (player != null && player.CarriedHandcuffs > 0)
        {
            Prisoner target = FindNearestFreePrisoner();
            if (target == null) break;

            // 해당 수감자에게 필요한 수갑이 부족하면 중단
            if (!player.UseCarriedHandcuff(target.requiredHandcuffs)) break;

            // 체포 후 돈 지급
            Transform cell = cellPoints.Length > 0
                ? cellPoints[cellIndex++ % cellPoints.Length]
                : null;
            if (cell != null) target.Arrest(cell);

            GameManager.Instance?.AddMoney(moneyPerArrest);
            UIManager.Instance?.ShowMoneyPopup(
                target.transform.position + Vector3.up * 1.2f, moneyPerArrest);

            yield return new WaitForSeconds(distributeInterval);
        }
        isDistributing = false;
    }

    Prisoner FindNearestFreePrisoner()
    {
        Prisoner nearest = null;
        float closest = float.MaxValue;
        foreach (var p in FindObjectsOfType<Prisoner>())
        {
            if (p.state != Prisoner.PrisonerState.Free) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d <= searchRadius && d < closest) { closest = d; nearest = p; }
        }
        return nearest;
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 업그레이드 존 (돈으로 구매)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class UpgradeZone : MonoBehaviour
{
    [Header("Upgrade")]
    // 구매 비용
    public int cost = 50;
    // 업그레이드 이름(설명용)
    public string upgradeName = "Mining Drill";
    // 구매 시 활성화할 오브젝트
    public GameObject unlockObject;

    [Header("UI")]
    // 월드 스페이스에 띄울 비용 표시 UI
    public GameObject upgradeUI;
    public TMPro.TextMeshPro costText;

    // 구매 여부 플래그
    private bool purchased = false;

    void Start()
    {
        // 비용 텍스트 초기화
        if (costText) costText.text = cost.ToString();
    }

    // 플레이어가 존에 머무르면 비용을 지불하고 업그레이드 수행
    void OnTriggerStay(Collider other)
    {
        if (purchased) return;
        if (other.GetComponent<PlayerController>() == null) return;

        if (GameManager.Instance.SpendMoney(cost))
        {
            purchased = true;
            if (upgradeUI) upgradeUI.SetActive(false);
            if (unlockObject) unlockObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
