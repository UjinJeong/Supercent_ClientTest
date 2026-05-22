using System.Collections;
using UnityEngine;

/// <summary>
/// 수감자 자동 스폰 및 처리 존
/// [동작 흐름]
///   테이블에 수갑이 있으면 → 수감자 스폰 (동시 maxConcurrentPrisoners명까지)
///   → 테이블 앞 이동 → 랜덤 수갑 소비 → 색상 변환 → 퇴장 → 돈 스폰
/// [부착 위치] PrisonerZone 오브젝트 (Trigger Collider 불필요)
/// </summary>
public class PrisonerZone : MonoBehaviour
{
    #region 인스펙터
    [Header("참조")]
    public HandcuffMoneyZone handcuffTable;  // 수갑이 쌓인 테이블 존
    public Transform         spawnPoint;     // 수감자가 생성될 위치
    public Transform         tablePoint;     // 수감자가 걸어올 테이블 앞 기준 위치
    public GameObject        prisonerPrefab; // 수감자 프리팹

    [Header("돈 생성")]
    public GameObject moneyPrefab;              // $ 존에 생성할 돈 프리팹
    public Transform  moneySpawnPoint;          // $ 존 기준 위치
    public float      moneyStackSpacing = 0.15f;// 돈 스택 세로 간격
    public int        moneyPerPrisoner  = 100;  // 수감자 처리 시 지급 금액

    [Header("스폰 설정")]
    public float spawnInterval         = 1.2f; // 수감자 스폰 간격 (초) ← 조절 포인트
    public int   maxConcurrentPrisoners = 3;   // 동시 처리 최대 수감자 수 ← 조절 포인트
    public float tablePosSpread        = 0.5f; // 테이블 앞 대기 위치 좌우 분산 폭

    [Header("처리 설정")]
    public int   maxDemand               = 5;    // 수감자가 요구하는 최대 수갑 수
    public float handcuffConsumeInterval = 0.3f; // 수갑 1개 소비 주기 (초)

    [Header("사운드")]
    public AudioClip processedSFX;
    public float     processedSFXVolume = 1f;
    #endregion

    #region 내부 변수
    private int  activePrisoners   = 0;     // 현재 활성 수감자 수
    private bool isTableOccupied   = false; // 테이블 점유 여부 — true면 모든 수감자 이동 정지
    private int  spawnedMoneyCount = 0;
    private float cachedMoneySpacing;       // 프리팹 실제 높이 기반 캐시 간격
    #endregion

    #region 생명주기
    void Start()
    {
        cachedMoneySpacing = StackUtils.CalcSpacing(moneyPrefab, moneyStackSpacing);
        StartCoroutine(SpawnLoop());
    }
    #endregion

    #region 스폰 루프
    /// <summary>
    /// spawnInterval마다 조건 확인 → 수갑이 있고 여유가 있으면 수감자 스폰
    /// ProcessPrisoner를 yield return 없이 시작 → 동시 다발 처리
    /// </summary>
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (handcuffTable == null)                            continue;
            if (handcuffTable.HandcuffCount <= 0)                 continue;
            if (activePrisoners >= maxConcurrentPrisoners)        continue;
            if (prisonerPrefab == null || spawnPoint == null
                                       || tablePoint == null)     continue;

            // yield return 없이 시작 → 루프가 블록되지 않아 연속 스폰 가능
            StartCoroutine(ProcessPrisoner());
        }
    }
    #endregion

    #region 수감자 처리
    IEnumerator ProcessPrisoner()
    {
        activePrisoners++;

        // 가용 수갑 범위 내 랜덤 요구량 결정 (최소 1 보장)
        int available = Mathf.Max(handcuffTable.HandcuffCount, 1);
        int demand    = Random.Range(1, Mathf.Min(maxDemand, available) + 1);

        // 수감자 스폰
        GameObject go       = Instantiate(prisonerPrefab, spawnPoint.position, spawnPoint.rotation);
        Prisoner   prisoner = go.GetComponent<Prisoner>();

        if (prisoner == null) { Destroy(go); activePrisoners--; yield break; }

        prisoner.SetDemand(demand);
        prisoner.SetState(PrisonerState.Walking);

        // 1. 테이블 앞으로 이동 — isTableOccupied 동안 WalkTo 내부에서 자동 정지
        float   offset   = Random.Range(-tablePosSpread, tablePosSpread);
        Vector3 tablePos = tablePoint.position + tablePoint.right * offset;
        yield return StartCoroutine(prisoner.WalkTo(tablePos, () => isTableOccupied));

        // 도착 후 테이블이 비워질 때까지 대기
        prisoner.SetState(PrisonerState.WaitingForTable);
        yield return new WaitUntil(() => !isTableOccupied);
        isTableOccupied = true;
        prisoner.SetState(PrisonerState.Processing);

        // 2. 수갑 1개씩 소비 — 수갑 없으면 생길 때까지 제자리 대기
        int remaining = demand;
        while (remaining > 0)
        {
            if (handcuffTable.HandcuffCount <= 0)
            {
                prisoner.SetState(PrisonerState.WaitingForHandcuffs);
                prisoner.SetWaiting(true);
                yield return new WaitUntil(() => handcuffTable != null
                                              && handcuffTable.HandcuffCount > 0);
                prisoner.SetWaiting(false);
                prisoner.SetState(PrisonerState.Processing);
                prisoner.UpdateDemand(remaining);
            }

            yield return new WaitForSeconds(handcuffConsumeInterval);

            if (handcuffTable.TryConsumeTableHandcuff())
            {
                remaining--;
                prisoner.UpdateDemand(remaining);
            }
        }

        // 3. 처리 완료 — 테이블 해제 + UI 숨김 + 색상 변환
        isTableOccupied = false;
        prisoner.SetState(PrisonerState.Served);
        prisoner.HideDemandUI();
        prisoner.ApplyProcessedColor();

        if (processedSFX != null)
            AudioSource.PlayClipAtPoint(processedSFX, transform.position, processedSFXVolume);

        // 4. 돈 생성
        SpawnMoney();
        GameManager.Instance.AddMoney(moneyPerPrisoner);

        // 5. 퇴장 후 Destroy
        prisoner.SetState(PrisonerState.Leaving);
        yield return StartCoroutine(prisoner.Leave());

        activePrisoners--;
    }
    #endregion

    #region 돈 스폰
    void SpawnMoney()
    {
        if (moneyPrefab == null || moneySpawnPoint == null) return;

        Vector3 pos = moneySpawnPoint.position
                    + Vector3.up * cachedMoneySpacing * spawnedMoneyCount;
        Instantiate(moneyPrefab, pos, moneySpawnPoint.rotation);
        spawnedMoneyCount++;
    }
    #endregion
}
