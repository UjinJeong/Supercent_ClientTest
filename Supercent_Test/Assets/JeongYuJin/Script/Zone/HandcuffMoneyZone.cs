using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 수갑 → 돈 변환 존
/// [진입] 플레이어의 수갑을 1개씩 소비 → outputPoint에 수갑 프리팹 스폰 + GameManager에 돈 추가
/// [부착 위치] HandcuffMoneyZone 오브젝트 (Trigger Collider 필수)
/// </summary>
public class HandcuffMoneyZone : MonoBehaviour
{
    #region 인스펙터
    [Header("참조")]
    public Transform  outputPoint;    // 수갑이 쌓일 기준점 (책상 등)
    public GameObject handcuffPrefab; // 시각용 수갑 프리팹

    [Header("사운드")]
    public AudioClip depositSFX;
    public float     depositSFXVolume = 1f;

    [Header("설정")]
    public float depositInterval  = 0.3f;  // 수갑 1개 처리 주기(초)
    public float stackSpacing     = 0.15f; // 쌓인 수갑 간 세로 간격
    public int   moneyPerHandcuff = 10;    // 수갑 1개당 지급 금액
    #endregion

    #region 내부 변수
    private PlayerController player;
    private Coroutine        depositRoutine;
    private List<GameObject> spawnedHandcuffs = new List<GameObject>();
    #endregion

    #region 트리거
    void OnTriggerEnter(Collider other)
    {
        PlayerController p = other.GetComponent<PlayerController>();
        if (p == null) return;

        player         = p;
        depositRoutine = StartCoroutine(DepositRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;

        if (depositRoutine != null)
        {
            StopCoroutine(depositRoutine);
            depositRoutine = null;
        }
        player = null;
    }
    #endregion

    #region 입금
    IEnumerator DepositRoutine()
    {
        while (true)
        {
            if (player == null || player.CarriedHandcuffs <= 0)
            {
                yield return null;
                continue;
            }

            // 수갑 1개 소비 → 책상에 스폰 → 돈 추가
            if (player.ConsumeHandcuff(1))
            {
                if (handcuffPrefab != null && outputPoint != null)
                {
                    Vector3    pos = outputPoint.position + Vector3.up * stackSpacing * spawnedHandcuffs.Count;
                    GameObject go  = Instantiate(handcuffPrefab, pos, outputPoint.rotation);
                    spawnedHandcuffs.Add(go);
                }

                GameManager.Instance.AddMoney(moneyPerHandcuff);

                if (depositSFX != null)
                    AudioSource.PlayClipAtPoint(depositSFX, transform.position, depositSFXVolume);
            }

            yield return new WaitForSeconds(depositInterval);
        }
    }
    #endregion
}
