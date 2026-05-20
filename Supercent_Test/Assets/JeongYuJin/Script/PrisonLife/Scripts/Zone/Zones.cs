using UnityEngine;
using System.Collections;

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

    // 죄수가 존에 들어오면 체포 처리(수갑 사용 및 최대 수용 검사)
    void OnTriggerEnter(Collider other)
    {
        Prisoner prisoner = other.GetComponent<Prisoner>();
        if (prisoner == null || prisoner.state != Prisoner.PrisonerState.Free) return;
        if (!GameManager.Instance.UseHandcuff()) return;
        if (GameManager.Instance.prisonerCount >= GameManager.Instance.maxPrisoners) return;

        Transform cell = cellPoints[cellIndex % cellPoints.Length];
        cellIndex++;
        prisoner.Arrest(cell);
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
