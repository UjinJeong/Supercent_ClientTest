using UnityEngine;
using System.Collections;

/// <summary>
/// 수갑 교환 존
/// 플레이어가 존에 진입하면 보유한 돌을 1개씩 소비하여
/// outputPoint(Handcuff 오브젝트) 위에 수갑 프리팹을 세로로 쌓는다.
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

    // ── 내부 변수 ────────────────────────────────────
    private PlayerController player;
    private Coroutine        exchangeRoutine;
    private int              handcuffCount = 0;  // 현재 outputPoint에 쌓인 수갑 수

    // ── 트리거 이벤트 ────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        // 플레이어가 진입하면 교환 코루틴 시작
        PlayerController p = other.GetComponent<PlayerController>();
        if (p == null) return;

        player          = p;
        exchangeRoutine = StartCoroutine(ExchangeRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        // 플레이어가 나가면 교환 중단
        if (other.GetComponent<PlayerController>() == null) return;

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
            // 플레이어가 없거나 돌이 없으면 대기
            if (player == null || player.CarriedMoney <= 0)
            {
                yield return null;
                continue;
            }

            // 돌 1개 소비 후 수갑 프리팹 스폰
            if (player.ConsumeRock(1) && handcuffPrefab != null && outputPoint != null)
            {
                Vector3 spawnPos = outputPoint.position + Vector3.up * stackSpacing * handcuffCount;
                Instantiate(handcuffPrefab, spawnPos, outputPoint.rotation);
                handcuffCount++;

                // 수갑 생성 효과음 재생
                if (spawnSFX != null)
                    AudioSource.PlayClipAtPoint(spawnSFX, outputPoint.position, spawnSFXVolume);
            }

            yield return new WaitForSeconds(exchangeInterval);
        }
    }
}
