using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 트리거 존 추상 기반 클래스
/// ─────────────────────────────────────
/// [역할] OnTriggerEnter/Exit + 코루틴 생명주기를 공통 처리
/// [상속] 새 존 추가 시 ZoneRoutine()만 구현하면 됨
/// [확장] OnPlayerEnter / OnPlayerExit 오버라이드로 진입·퇴장 후처리 추가 가능
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class BaseZone : MonoBehaviour
{
    #region 내부 변수
    protected PlayerController player;        // 현재 존 안에 있는 플레이어
    private   Coroutine        activeRoutine; // 실행 중인 ZoneRoutine 코루틴 핸들
    #endregion

    #region 트리거 (공통)
    void OnTriggerEnter(Collider other)
    {
        // 이미 다른 플레이어가 처리 중이면 새 진입 무시 (다중 플레이어 방지)
        if (player != null) return;

        // PlayerController가 없는 오브젝트는 무시 (적, 장식물 등)
        PlayerController p = other.GetComponent<PlayerController>();
        if (p == null) return;

        player        = p;
        activeRoutine = StartCoroutine(ZoneRoutine()); // 존 로직 시작
        OnPlayerEnter(p);                              // 상속 클래스 후처리 훅
    }

    void OnTriggerExit(Collider other)
    {
        // StopZoneRoutine()이 먼저 호출되어 player가 이미 null이면 중복 정리 방지
        if (player == null) return;

        // 퇴장한 Collider가 현재 처리 중인 플레이어가 아니면 무시
        if (other.GetComponent<PlayerController>() != player) return;

        StopZoneRoutine(); // 코루틴 중지 + player 초기화
        OnPlayerExit();    // 상속 클래스 후처리 훅
    }
    #endregion

    #region 유틸
    /// <summary>
    /// 코루틴 중지 + player 참조 해제
    /// 상속 클래스에서 직접 종료가 필요할 때 호출 (예: PickupHandcuffs 후 즉시 중단)
    /// </summary>
    protected void StopZoneRoutine()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        player = null;
    }
    #endregion

    #region 추상 / 가상 메서드
    /// <summary>
    /// 플레이어가 존 안에 있는 동안 반복 실행되는 핵심 로직
    /// 반드시 상속 클래스에서 구현해야 함
    /// </summary>
    protected abstract IEnumerator ZoneRoutine();

    /// <summary>플레이어 진입 직후 추가 처리가 필요할 때 오버라이드 (기본값: 빈 동작)</summary>
    protected virtual void OnPlayerEnter(PlayerController p) { }

    /// <summary>플레이어 퇴장 직후 추가 처리가 필요할 때 오버라이드 (기본값: 빈 동작)</summary>
    protected virtual void OnPlayerExit() { }
    #endregion
}
