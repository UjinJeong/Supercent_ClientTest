using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 수갑 테이블 존
/// [진입] 플레이어의 수갑을 1개씩 소비 → outputPoint에 수갑 프리팹 스폰
/// [수감자] PrisonerZone이 HandcuffCount / TryConsumeTableHandcuff()로 수갑을 가져감
///          → 돈 지급은 PrisonerZone(수감자 처리 완료 시)이 담당
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
    public float depositInterval = 0.3f;  // 수갑 1개 처리 주기(초)
    public float stackSpacing    = 0.15f; // 쌓인 수갑 간 세로 간격
    #endregion

    #region 공개 프로퍼티 (PrisonerZone용)
    /// <summary>현재 테이블 위 수갑 수</summary>
    public int HandcuffCount => spawnedHandcuffs.Count;
    #endregion

    #region 내부 변수
    private PlayerController player;
    private Coroutine        depositRoutine;
    private List<GameObject> spawnedHandcuffs = new List<GameObject>();
    private float            cachedSpacing;   // 프리팹 실제 높이 기반 캐시 간격
    #endregion

    #region 생명주기
    void Start()
    {
        cachedSpacing = CalcSpacing(handcuffPrefab, stackSpacing);
    }
    #endregion

    #region 간격 계산
    /// <summary>프리팹 Renderer 실제 높이를 반영한 스택 간격 계산</summary>
    float CalcSpacing(GameObject prefab, float baseSpacing)
    {
        float spacing = Mathf.Max(0.01f, baseSpacing);
        if (prefab == null) return spacing;
        var r = prefab.GetComponentInChildren<Renderer>();
        if (r != null && r.bounds.size.y > 0f)
            spacing = Mathf.Max(spacing, r.bounds.size.y * 0.9f);
        return spacing;
    }
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

    #region 수감자 소비 (PrisonerZone 호출용)
    /// <summary>테이블에서 수갑 1개 제거 — 성공 시 true 반환</summary>
    public bool TryConsumeTableHandcuff()
    {
        if (spawnedHandcuffs.Count == 0) return false;

        int last = spawnedHandcuffs.Count - 1;
        if (spawnedHandcuffs[last] != null)
            Destroy(spawnedHandcuffs[last]);

        spawnedHandcuffs.RemoveAt(last);
        return true;
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

            // 수갑 1개 소비 → 책상에 스폰 (돈 지급은 PrisonerZone이 담당)
            if (player.ConsumeHandcuff(1))
            {
                if (handcuffPrefab != null && outputPoint != null)
                {
                    Vector3    pos = outputPoint.position + Vector3.up * cachedSpacing * spawnedHandcuffs.Count;
                    GameObject go  = Instantiate(handcuffPrefab, pos, outputPoint.rotation);
                    spawnedHandcuffs.Add(go);
                }

                if (depositSFX != null)
                    AudioSource.PlayClipAtPoint(depositSFX, transform.position, depositSFXVolume);
            }

            yield return new WaitForSeconds(depositInterval);
        }
    }
    #endregion
}
