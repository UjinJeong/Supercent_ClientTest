using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>수감자 현재 처리 단계 — Inspector에서 실시간 확인 가능</summary>
public enum PrisonerState
{
    Walking,             // 테이블로 이동 중
    WaitingForTable,     // 테이블 앞 대기 (다른 수감자 점유 중)
    Processing,          // 수갑 소비 중
    WaitingForHandcuffs, // 수갑 보충 대기
    Served,              // 처리 완료
    Leaving              // 퇴장 중
}

/// <summary>
/// 수감자 개체 — 이동 / 수갑 요구 UI / 색상 변환 / 퇴장
/// ───────────────────────────────────────────────────
/// [부착 위치] Prisoner 프리팹 루트
/// [제어 주체] PrisonerZone이 코루틴을 순서대로 호출하며 SetState()로 상태를 전환
/// [역할 분리] 이 클래스는 "어떻게" 움직이고 표시할지만 알고,
///             "언제·왜" 처리되는지는 PrisonerZone이 결정
/// </summary>
public class Prisoner : MonoBehaviour
{
    #region 인스펙터
    [Header("이동")]
    public float moveSpeed    = 3f; // 테이블로 걸어가는 속도
    public float leaveSpeed   = 2f; // 퇴장 시 이동 속도 (처리 후 천천히 걸어나감)
    public float leaveDistance = 3f; // 퇴장 거리 (앞 방향으로 얼마나 이동 후 Destroy)

    [Header("시각")]
    public Color processedColor = new Color(0.4f, 0.6f, 1f); // 처리 완료 후 몸 색상 (파란 계열)

    [Header("UI 참조")]
    public GameObject      demandUI;   // 머리 위 UI 루트 (Canvas 또는 Image 부모)
    public TextMeshProUGUI demandText; // 요구 수갑 수 텍스트
    #endregion

    #region 상태 머신
    [Header("현재 상태 (디버그)")]
    [SerializeField] private PrisonerState currentState = PrisonerState.Walking; // Inspector 실시간 확인용

    /// <summary>현재 상태 읽기 전용 접근자 — 외부에서 SetState() 없이 직접 수정 불가</summary>
    public PrisonerState State => currentState;

    /// <summary>PrisonerZone이 처리 단계마다 호출해 상태를 전환 — 단일 진입점으로 일관성 유지</summary>
    public void SetState(PrisonerState newState) => currentState = newState;
    #endregion

    #region 생명주기
    void LateUpdate()
    {
        // 머리 위 UI가 항상 카메라 방향을 향하도록 회전 동기화
        // LateUpdate 사용: 카메라 이동이 완료된 이후에 회전을 맞춤
        if (demandUI != null && demandUI.activeSelf && Camera.main != null)
            demandUI.transform.rotation = Camera.main.transform.rotation;
    }
    #endregion

    #region UI
    /// <summary>요구 수갑 수 초기 설정 및 UI 표시 — 수감자 스폰 직후 PrisonerZone이 호출</summary>
    public void SetDemand(int count)
    {
        if (demandText != null) demandText.text = count.ToString(); // 숫자 표시
        if (demandUI   != null) demandUI.SetActive(true);           // UI 패널 활성화
    }

    /// <summary>수갑 1개 소비될 때마다 남은 수 갱신 — PrisonerZone 처리 루프에서 매 소비 후 호출</summary>
    public void UpdateDemand(int remaining)
    {
        if (demandText != null) demandText.text = remaining.ToString();
    }

    /// <summary>처리 완료 후 UI 숨김 — Served 상태 진입 시 PrisonerZone이 호출</summary>
    public void HideDemandUI()
    {
        if (demandUI != null) demandUI.SetActive(false);
    }

    /// <summary>
    /// 수갑 대기 상태 시각 표시
    /// isWaiting=true  → 텍스트 회색 + "..." (수갑 보충 기다리는 중)
    /// isWaiting=false → 텍스트 흰색 + 남은 수 복원 (PrisonerZone이 UpdateDemand로 재설정)
    /// </summary>
    public void SetWaiting(bool isWaiting)
    {
        if (demandText == null) return;
        demandText.color = isWaiting ? Color.gray : Color.white; // 색상으로 대기 여부 시각화
        if (isWaiting) demandText.text = "...";                  // 대기 중 애니메이션 대체 텍스트
    }
    #endregion

    #region 색상
    /// <summary>
    /// 처리 완료 색상 적용 — Served 상태 진입 시 PrisonerZone이 호출
    /// Standard 셰이더(_Color) / URP 셰이더(_BaseColor) 모두 대응
    /// r.material: 공유 머티리얼이 아닌 인스턴스를 수정해 다른 수감자에 영향 없음
    /// </summary>
    public void ApplyProcessedColor()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            Material mat = r.material; // 인스턴스 생성 (원본 머티리얼 보호)
            if      (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", processedColor); // URP
            else if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     processedColor); // Standard
        }
    }
    #endregion

    #region 이동
    /// <summary>
    /// 목표 지점까지 걸어가기 — PrisonerZone에서 yield return으로 완료까지 대기
    /// shouldPause가 true를 반환하는 동안 이동 없이 제자리 대기 (테이블 점유 시 사용)
    /// 목표 도달(거리 ≤ 0.05f) 후 정확한 위치로 스냅
    /// </summary>
    public IEnumerator WalkTo(Vector3 target, System.Func<bool> shouldPause = null)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            // 정지 조건(isTableOccupied)이 true면 이동 없이 다음 프레임 대기
            if (shouldPause != null && shouldPause())
            {
                yield return null;
                continue;
            }

            // 진행 방향으로 부드럽게 회전 (Slerp: 10f = 빠른 보간)
            Vector3 dir = (target - transform.position);
            if (dir.magnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), 10f * Time.deltaTime);

            // 속도에 맞춰 한 프레임 이동
            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime);
            yield return null; // 다음 프레임까지 대기
        }

        transform.position = target; // 부동소수점 오차 제거 — 정확한 목표 위치로 스냅
    }

    /// <summary>
    /// 앞 방향으로 leaveDistance만큼 이동 후 자기 자신 Destroy
    /// Served 완료 이후 PrisonerZone이 yield return으로 완료까지 대기
    /// </summary>
    public IEnumerator Leave()
    {
        // 현재 앞 방향(forward) 기준으로 목표 지점 계산 — 씬 밖으로 걸어나가는 효과
        Vector3 target = transform.position + transform.forward * leaveDistance;

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, leaveSpeed * Time.deltaTime); // 퇴장 속도로 이동
            yield return null; // 다음 프레임까지 대기
        }

        Destroy(gameObject); // 씬 이탈 완료 → 오브젝트 제거
    }
    #endregion
}
