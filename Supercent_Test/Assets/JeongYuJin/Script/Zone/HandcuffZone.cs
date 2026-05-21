using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 수갑 교환 존
/// [진입] 보유한 돌을 1개씩 소비 → outputPoint에 수갑 프리팹 세로로 스폰
/// [픽업] 플레이어가 outputPoint에 pickupDistance 이내로 접근하면 수갑을 플레이어에 저장
///        (존 안팎 상관없이 OverlapSphere로 탐지)
/// [부착 위치] HandcuffZone 오브젝트 (Trigger Collider 필수)
/// </summary>
public class HandcuffZone : MonoBehaviour
{
    // ── 인스펙터 ─────────────────────────────────────
    [Header("참조")]
    public Transform  outputPoint;      // 수갑이 쌓일 기준점 (Handcuff 오브젝트)
    public GameObject handcuffPrefab;   // 생성할 수갑 프리팹

    [Header("사운드")]
    public AudioClip spawnSFX;          // 수갑 생성 효과음
    public float     spawnSFXVolume = 1f;

    [Header("설정")]
    public float exchangeInterval = 0.3f;   // 돌 1개 → 수갑 1개 변환 주기(초)
    public float stackSpacing     = 0.4f;   // 수갑 프리팹 간 세로 간격
    public float pickupDistance   = 1.5f;   // 수갑 픽업이 시작되는 outputPoint까지의 거리

    // ── 내부 변수 ────────────────────────────────────
    private PlayerController     player;            // 존 안의 플레이어 (교환용)
    private Coroutine            exchangeRoutine;
    private List<GameObject>     spawnedHandcuffs = new List<GameObject>();

    // ── 유니티 생명주기 ──────────────────────────────
    void Update()
    {
        // 쌓인 수갑이 없거나 outputPoint가 없으면 체크 불필요
        if (spawnedHandcuffs.Count == 0 || outputPoint == null) return;

        // 트리거 상태와 무관하게 outputPoint 주변을 직접 탐색
        // → 존 안팎 어디서든 다가오면 픽업 가능
        Collider[] hits = Physics.OverlapSphere(outputPoint.position, pickupDistance);
        foreach (var hit in hits)
        {
            PlayerController p = hit.GetComponent<PlayerController>();
            if (p == null) continue;
            PickupHandcuffs(p);
            return;
        }
    }

    // ── 트리거 이벤트 ────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        // 플레이어가 존에 진입하면 교환 코루틴 시작
        PlayerController p = other.GetComponent<PlayerController>();
        if (p == null) return;

        player          = p;
        exchangeRoutine = StartCoroutine(ExchangeRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;

        // 존을 벗어나면 교환 중단, player 초기화
        if (exchangeRoutine != null)
        {
            StopCoroutine(exchangeRoutine);
            exchangeRoutine = null;
        }
        player = null;
    }

    // ── 교환 코루틴 ──────────────────────────────────
    /// <summary>
    /// 플레이어가 존 안에 있는 동안 돌을 1개씩 소비하며
    /// outputPoint 위에 수갑을 세로로 쌓는다.
    /// </summary>
    IEnumerator ExchangeRoutine()
    {
        while (true)
        {
            if (player == null || player.CarriedMoney <= 0)
            {
                yield return null;
                continue;
            }

            if (player.ConsumeRock(1) && handcuffPrefab != null && outputPoint != null)
            {
                Vector3    spawnPos = outputPoint.position + Vector3.up * stackSpacing * spawnedHandcuffs.Count;
                GameObject go       = Instantiate(handcuffPrefab, spawnPos, outputPoint.rotation);
                spawnedHandcuffs.Add(go);

                if (spawnSFX != null)
                    AudioSource.PlayClipAtPoint(spawnSFX, outputPoint.position, spawnSFXVolume);
            }

            yield return new WaitForSeconds(exchangeInterval);
        }
    }

    // ── 픽업 ─────────────────────────────────────────
    /// <summary>
    /// 스폰된 수갑을 전부 제거하고 플레이어 등에 추가한다.
    /// 교환 코루틴도 중단해 픽업 후 새로 캔 돌이 즉시 변환되지 않도록 한다.
    /// </summary>
    void PickupHandcuffs(PlayerController p)
    {
        p.AddHandcuffToCarry(spawnedHandcuffs.Count);

        foreach (var go in spawnedHandcuffs)
            if (go != null) Destroy(go);
        spawnedHandcuffs.Clear();

        // 픽업 후 교환도 중단 (존 안에 있더라도 재진입 전까지 비활성화)
        if (exchangeRoutine != null)
        {
            StopCoroutine(exchangeRoutine);
            exchangeRoutine = null;
        }
        player = null;
    }
}
