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
/// [부착 위치] Prisoner 프리팹 루트
/// PrisonerZone이 코루틴을 순서대로 호출하며 SetState()로 상태를 전환한다
/// </summary>
public class Prisoner : MonoBehaviour
{
    #region 인스펙터
    [Header("이동")]
    public float moveSpeed  = 3f;  // 등장 이동 속도
    public float leaveSpeed = 2f;  // 퇴장 이동 속도
    public float leaveDistance = 3f; // 퇴장 거리

    [Header("시각")]
    public Color processedColor = new Color(0.4f, 0.6f, 1f); // 처리 완료 후 색상

    [Header("UI 참조")]
    public GameObject      demandUI;   // 머리 위 UI 루트 (Canvas 또는 Image)
    public TextMeshProUGUI demandText; // 요구 수갑 수 텍스트
    #endregion

    #region 상태 머신
    [Header("현재 상태 (디버그)")]
    [SerializeField] private PrisonerState currentState = PrisonerState.Walking;

    public PrisonerState State => currentState;

    /// <summary>PrisonerZone이 처리 단계마다 호출해 상태를 전환</summary>
    public void SetState(PrisonerState newState) => currentState = newState;
    #endregion

    #region 생명주기
    void LateUpdate()
    {
        // 머리 위 UI가 항상 카메라를 향하도록
        if (demandUI != null && demandUI.activeSelf && Camera.main != null)
            demandUI.transform.rotation = Camera.main.transform.rotation;
    }
    #endregion

    #region UI
    /// <summary>요구 수갑 수 초기 설정 및 UI 표시</summary>
    public void SetDemand(int count)
    {
        if (demandText != null) demandText.text = count.ToString();
        if (demandUI   != null) demandUI.SetActive(true);
    }

    /// <summary>남은 수갑 수 갱신</summary>
    public void UpdateDemand(int remaining)
    {
        if (demandText != null) demandText.text = remaining.ToString();
    }

    /// <summary>처리 완료 후 UI 숨김</summary>
    public void HideDemandUI()
    {
        if (demandUI != null) demandUI.SetActive(false);
    }

    /// <summary>수갑 대기 상태 시각 표시 — 대기 중: 텍스트 회색 + "..." / 재개: 원래대로</summary>
    public void SetWaiting(bool isWaiting)
    {
        if (demandText == null) return;
        demandText.color = isWaiting ? Color.gray : Color.white;
        if (isWaiting) demandText.text = "...";
    }
    #endregion

    #region 색상
    /// <summary>처리 완료 색상 적용 (Standard / URP 셰이더 모두 대응)</summary>
    public void ApplyProcessedColor()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            Material mat = r.material; // 인스턴스 생성
            if      (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", processedColor);
            else if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     processedColor);
        }
    }
    #endregion

    #region 이동
    /// <summary>
    /// 목표 지점까지 걸어가기 (PrisonerZone에서 yield return)
    /// shouldPause가 true를 반환하는 동안 제자리 정지
    /// </summary>
    public IEnumerator WalkTo(Vector3 target, System.Func<bool> shouldPause = null)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            // 정지 조건이 true면 이동 없이 대기
            if (shouldPause != null && shouldPause())
            {
                yield return null;
                continue;
            }

            Vector3 dir = (target - transform.position);
            if (dir.magnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), 10f * Time.deltaTime);

            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    /// <summary>앞 방향으로 leaveDistance만큼 이동 후 자기 자신 Destroy</summary>
    public IEnumerator Leave()
    {
        Vector3 target = transform.position + transform.forward * leaveDistance;
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, leaveSpeed * Time.deltaTime);
            yield return null;
        }
        Destroy(gameObject);
    }
    #endregion
}
