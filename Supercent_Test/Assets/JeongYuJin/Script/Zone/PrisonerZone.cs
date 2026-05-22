using System.Collections;
using UnityEngine;

/// <summary>
/// 수감자 자동 스폰 및 처리 존
/// ──────────────────────────────────────────────────────────
/// [동작 흐름]
///   테이블에 수갑이 있으면 → 수감자 스폰 (동시 maxConcurrentPrisoners명까지)
///   → 테이블 앞 이동 → 랜덤 수갑 소비 → 색상 변환 → 퇴장 → 돈 스폰
/// [부착 위치] PrisonerZone 오브젝트 (Trigger Collider 불필요)
/// [동시성]   SpawnLoop는 ProcessPrisoner를 yield return 없이 시작 → 여러 수감자 병렬 처리
/// </summary>
public class PrisonerZone : MonoBehaviour
{
    #region 인스펙터
    [Header("참조")]
    public HandcuffMoneyZone handcuffTable;  // 수갑이 쌓인 테이블 존 (HandcuffCount·ConsumeHandcuff 제공)
    public Transform         spawnPoint;     // 수감자가 생성될 위치
    public Transform         tablePoint;     // 수감자가 걸어올 테이블 앞 기준 위치
    public GameObject        prisonerPrefab; // 수감자 프리팹

    [Header("돈 생성")]
    public GameObject moneyPrefab;               // $ 존에 생성할 돈 프리팹
    public Transform  moneySpawnPoint;           // $ 존 기준 위치
    public float      moneyStackSpacing = 0.15f; // 돈 스택 세로 간격 ← 조절 포인트
    public int        moneyPerPrisoner  = 100;   // 수감자 처리 시 GameManager에 추가할 금액

    [Header("스폰 설정")]
    public float spawnInterval         = 1.2f; // 수감자 스폰 간격 (초) ← 조절 포인트
    public int   maxConcurrentPrisoners = 3;   // 동시 처리 최대 수감자 수 ← 조절 포인트
    public float tablePosSpread        = 0.5f; // 테이블 앞 대기 위치 좌우 분산 폭 (겹침 방지)

    [Header("처리 설정")]
    public int   maxDemand               = 5;    // 수감자가 요구하는 최대 수갑 수 ← 조절 포인트
    public float handcuffConsumeInterval = 0.3f; // 수갑 1개 소비 주기 (초) ← 조절 포인트

    [Header("사운드")]
    public AudioClip processedSFX;          // 수감자 처리 완료 효과음
    public float     processedSFXVolume = 1f;
    #endregion

    #region 내부 변수
    private int   activePrisoners   = 0;     // 현재 처리 중인 수감자 수 (최대값 초과 시 스폰 차단)
    private bool  isTableOccupied   = false; // 테이블 점유 여부 — true면 모든 수감자 이동 정지
    private int   spawnedMoneyCount = 0;     // 스폰된 돈 누적 수 (스택 높이 계산에 사용)
    private float cachedMoneySpacing;        // 프리팹 실제 높이 기반 캐시 간격 (Start에서 1회 계산)
    #endregion

    #region 생명주기
    void Start()
    {
        // 돈 프리팹의 Renderer 높이를 반영한 스택 간격 캐시 (매 프레임 계산 방지)
        cachedMoneySpacing = StackUtils.CalcSpacing(moneyPrefab, moneyStackSpacing);
        StartCoroutine(SpawnLoop()); // 게임 시작과 함께 스폰 루프 시작
    }
    #endregion

    #region 스폰 루프
    /// <summary>
    /// spawnInterval마다 조건 확인 → 수갑이 있고 여유가 있으면 수감자 스폰
    /// ProcessPrisoner를 yield return 없이 시작 → 루프가 블록되지 않아 연속 스폰 가능
    /// </summary>
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval); // 스폰 간격 대기

            if (handcuffTable == null)                            continue; // 테이블 미연결 시 스킵
            if (handcuffTable.HandcuffCount <= 0)                 continue; // 테이블에 수갑 없으면 스킵
            if (activePrisoners >= maxConcurrentPrisoners)        continue; // 동시 처리 한도 초과 시 스킵
            if (prisonerPrefab == null || spawnPoint == null
                                       || tablePoint == null)     continue; // 필수 참조 누락 시 스킵

            // yield return 없이 시작 → 이 루프가 블록되지 않아 다음 interval에 또 스폰 가능
            StartCoroutine(ProcessPrisoner());
        }
    }
    #endregion

    #region 수감자 처리
    IEnumerator ProcessPrisoner()
    {
        activePrisoners++; // 처리 시작 시 카운터 증가 (SpawnLoop 스폰 제한에 반영)

        // ── 요구량 결정 ──────────────────────────────────────────────────
        // 현재 테이블 수갑 수 범위 내에서 랜덤 결정 (최소 1 보장)
        int available = Mathf.Max(handcuffTable.HandcuffCount, 1);
        int demand    = Random.Range(1, Mathf.Min(maxDemand, available) + 1);

        // ── 수감자 스폰 ──────────────────────────────────────────────────
        GameObject go       = Instantiate(prisonerPrefab, spawnPoint.position, spawnPoint.rotation);
        Prisoner   prisoner = go.GetComponent<Prisoner>();

        // Prisoner 컴포넌트 누락 방어 — 잘못된 프리팹 사용 시 즉시 정리
        if (prisoner == null) { Destroy(go); activePrisoners--; yield break; }

        prisoner.SetDemand(demand);               // UI에 요구 수갑 수 표시
        prisoner.SetState(PrisonerState.Walking); // 이동 상태로 초기화

        // ── 1. 테이블 앞으로 이동 ─────────────────────────────────────────
        // tablePosSpread 범위 내 좌우 오프셋 → 여러 수감자가 동일 위치 겹침 방지
        float   offset   = Random.Range(-tablePosSpread, tablePosSpread);
        Vector3 tablePos = tablePoint.position + tablePoint.right * offset;

        // WalkTo의 shouldPause 콜백: isTableOccupied == true인 동안 이동 정지
        // → 테이블을 다른 수감자가 사용 중이면 이동 없이 그 자리에서 대기
        yield return StartCoroutine(prisoner.WalkTo(tablePos, () => isTableOccupied));

        // ── 2. 테이블 점유 대기 → 점유 ───────────────────────────────────
        prisoner.SetState(PrisonerState.WaitingForTable);

        // 도착했어도 다른 수감자가 처리 중이면 차례가 올 때까지 대기
        yield return new WaitUntil(() => !isTableOccupied);
        isTableOccupied = true;                         // 테이블 점유 선언 (다른 수감자 이동 정지)
        prisoner.SetState(PrisonerState.Processing);

        // ── 3. 수갑 1개씩 소비 ───────────────────────────────────────────
        int remaining = demand;
        while (remaining > 0)
        {
            // 테이블 수갑 소진 시 — 보충될 때까지 대기 (WaitUntil로 폴링 없이 효율적 대기)
            if (handcuffTable.HandcuffCount <= 0)
            {
                prisoner.SetState(PrisonerState.WaitingForHandcuffs);
                prisoner.SetWaiting(true); // UI: "..." 표시
                yield return new WaitUntil(() => handcuffTable != null
                                              && handcuffTable.HandcuffCount > 0);
                prisoner.SetWaiting(false);                      // UI: 원래 숫자로 복원
                prisoner.SetState(PrisonerState.Processing);
                prisoner.UpdateDemand(remaining);               // 남은 수량 재표시
            }

            yield return new WaitForSeconds(handcuffConsumeInterval); // 소비 간격 대기

            // 실제 소비 — TryConsumeTableHandcuff 실패 시 카운트 감소 없음
            if (handcuffTable.TryConsumeTableHandcuff())
            {
                remaining--;
                prisoner.UpdateDemand(remaining); // UI 갱신
            }
        }

        // ── 4. 처리 완료 ─────────────────────────────────────────────────
        isTableOccupied = false;                  // 테이블 해제 → 다음 수감자 이동 재개
        prisoner.SetState(PrisonerState.Served);
        prisoner.HideDemandUI();                  // 요구 UI 숨김
        prisoner.ApplyProcessedColor();           // 처리 완료 색상 적용

        if (processedSFX != null)
            AudioSource.PlayClipAtPoint(processedSFX, transform.position, processedSFXVolume);

        // ── 5. 보상 지급 ─────────────────────────────────────────────────
        SpawnMoney();                             // 돈 오브젝트 씬에 생성
        GameManager.Instance.AddMoney(moneyPerPrisoner); // GameManager 금액 증가

        // ── 6. 퇴장 후 Destroy ────────────────────────────────────────────
        prisoner.SetState(PrisonerState.Leaving);
        yield return StartCoroutine(prisoner.Leave()); // 앞 방향으로 이동 후 자동 Destroy

        activePrisoners--; // 처리 완료 시 카운터 감소 (다음 스폰 허용)
    }
    #endregion

    #region 돈 스폰
    /// <summary>
    /// 수감자 처리 완료 시 moneySpawnPoint 위에 돈 1개 스폰
    /// spawnedMoneyCount를 높이 인덱스로 사용 → 쌓인 스택 모양 연출
    /// </summary>
    void SpawnMoney()
    {
        if (moneyPrefab == null || moneySpawnPoint == null) return;

        // 누적 돈 수 × 간격만큼 위로 올려서 스폰 (이미 쌓인 돈 위에 추가)
        Vector3 pos = moneySpawnPoint.position
                    + Vector3.up * cachedMoneySpacing * spawnedMoneyCount;
        Instantiate(moneyPrefab, pos, moneySpawnPoint.rotation);
        spawnedMoneyCount++; // 다음 돈은 그 위에 쌓임
    }
    #endregion
}
