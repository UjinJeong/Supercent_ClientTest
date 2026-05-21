using UnityEngine;
using System.Collections;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 돈 입금 존 — 플레이어가 진입하면 스택에 쌓인 묶음을 하나씩 입금
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public class MoneyDepositZone : MonoBehaviour
{
    [Header("Deposit")]
    public float depositInterval = 0.1f;

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
        if (other.GetComponent<PlayerController>() == null) return;
        player = null;
        isDepositing = false;
        StopAllCoroutines();
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
