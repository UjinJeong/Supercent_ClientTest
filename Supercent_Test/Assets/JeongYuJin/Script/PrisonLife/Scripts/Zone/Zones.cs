using UnityEngine;
using System.Collections;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 돈 입금 존 (책상 앞에서 돈 제출)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class MoneyDepositZone : MonoBehaviour
{
    [Header("Deposit")]
    public float depositInterval = 0.1f;  // 돈 하나씩 입금되는 속도

    private PlayerController player;
    private bool isDepositing = false;

    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;
        player = pc;
        if (!isDepositing) StartCoroutine(DepositRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            player = null;
            isDepositing = false;
            StopAllCoroutines();
        }
    }

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
    public int handcuffAmount = 4;
    public float respawnTime = 10f;
    public GameObject visualObject;

    private bool available = true;

    void OnTriggerEnter(Collider other)
    {
        if (!available) return;
        if (other.GetComponent<PlayerController>() == null) return;

        GameManager.Instance.AddHandcuff(handcuffAmount);
        available = false;
        if (visualObject) visualObject.SetActive(false);
        StartCoroutine(Respawn());
    }

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
    public Transform[] cellPoints;

    private int cellIndex = 0;

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
    public int cost = 50;
    public string upgradeName = "Mining Drill";
    public GameObject unlockObject;       // 업그레이드 후 활성화할 오브젝트

    [Header("UI")]
    public GameObject upgradeUI;          // 비용 표시 UI (Canvas World Space)
    public TMPro.TextMeshPro costText;

    private bool purchased = false;

    void Start()
    {
        if (costText) costText.text = cost.ToString();
    }

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
