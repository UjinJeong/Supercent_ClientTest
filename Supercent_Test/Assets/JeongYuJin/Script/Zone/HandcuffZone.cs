using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 수갑 교환 존
/// [진입] 보유한 돌 1개씩 소비 → outputPoint에 수갑 프리팹 세로로 스폰
/// [픽업] 플레이어가 outputPoint에 pickupDistance 이내로 접근하면 수갑을 플레이어에 저장
/// [부착 위치] HandcuffZone 오브젝트 (Trigger Collider 필수)
/// </summary>
public class HandcuffZone : MonoBehaviour
{
    #region 인스펙터
    [Header("참조")]
    public Transform  outputPoint;    // 수갑이 쌓일 기준점 (Handcuff 오브젝트)
    public GameObject handcuffPrefab;

    [Header("사운드")]
    public AudioClip spawnSFX;
    public float     spawnSFXVolume = 1f;

    [Header("설정")]
    public float exchangeInterval = 0.3f;  // 돌 1개 → 수갑 1개 변환 주기(초)
    public float stackSpacing     = 0.4f;  // 수갑 간 세로 간격
    public float pickupDistance   = 1.5f;  // outputPoint 기준 픽업 인식 거리
    #endregion

    #region 내부 변수
    private PlayerController  player;
    private Coroutine         exchangeRoutine;
    private List<GameObject>  spawnedHandcuffs = new List<GameObject>();
    #endregion

    #region 생명주기
    void Update()
    {
        if (spawnedHandcuffs.Count == 0 || outputPoint == null) return;

        // 트리거 상태 무관하게 outputPoint 주변을 직접 탐색 → 픽업
        Collider[] hits = Physics.OverlapSphere(outputPoint.position, pickupDistance);
        foreach (var hit in hits)
        {
            PlayerController p = hit.GetComponent<PlayerController>();
            if (p == null) continue;
            PickupHandcuffs(p);
            return;
        }
    }
    #endregion

    #region 트리거
    void OnTriggerEnter(Collider other)
    {
        PlayerController p = other.GetComponent<PlayerController>();
        if (p == null) return;

        player          = p;
        exchangeRoutine = StartCoroutine(ExchangeRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;

        if (exchangeRoutine != null)
        {
            StopCoroutine(exchangeRoutine);
            exchangeRoutine = null;
        }
        player = null;
    }
    #endregion

    #region 교환
    IEnumerator ExchangeRoutine()
    {
        while (true)
        {
            if (player == null || player.CarriedMoney <= 0) { yield return null; continue; }

            if (player.ConsumeRock(1) && handcuffPrefab != null && outputPoint != null)
            {
                Vector3    pos = outputPoint.position + Vector3.up * stackSpacing * spawnedHandcuffs.Count;
                GameObject go  = Instantiate(handcuffPrefab, pos, outputPoint.rotation);
                spawnedHandcuffs.Add(go);

                if (spawnSFX != null)
                    AudioSource.PlayClipAtPoint(spawnSFX, outputPoint.position, spawnSFXVolume);
            }

            yield return new WaitForSeconds(exchangeInterval);
        }
    }
    #endregion

    #region 픽업
    void PickupHandcuffs(PlayerController p)
    {
        p.AddHandcuffToCarry(spawnedHandcuffs.Count);

        foreach (var go in spawnedHandcuffs)
            if (go != null) Destroy(go);
        spawnedHandcuffs.Clear();

        // 픽업 후 교환 중단 — 재진입 전까지 변환 비활성화
        if (exchangeRoutine != null)
        {
            StopCoroutine(exchangeRoutine);
            exchangeRoutine = null;
        }
        player = null;
    }
    #endregion
}
