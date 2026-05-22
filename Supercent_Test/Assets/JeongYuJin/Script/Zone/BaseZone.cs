using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 트리거 존 추상 기반 클래스
/// ─────────────────────────────────────
/// [역할] OnTriggerEnter/Exit + 코루틴 생명주기를 공통 처리
/// [상속] 새 존 추가 시 ZoneRoutine()만 구현하면 됨
/// [확장] OnPlayerEnter / OnPlayerExit 오버라이드로 진입/퇴장 후처리 가능
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class BaseZone : MonoBehaviour
{
    #region 내부 변수
    protected PlayerController player;
    private   Coroutine        activeRoutine;
    #endregion

    #region 트리거 (공통)
    void OnTriggerEnter(Collider other)
    {
        if (player != null) return; // 이미 처리 중인 플레이어 있으면 무시
        PlayerController p = other.GetComponent<PlayerController>();
        if (p == null) return;

        player        = p;
        activeRoutine = StartCoroutine(ZoneRoutine());
        OnPlayerEnter(p);
    }

    void OnTriggerExit(Collider other)
    {
        // player가 null이면 이미 StopZoneRoutine()으로 정리된 상태 → 무시
        if (player == null) return;
        if (other.GetComponent<PlayerController>() != player) return;

        StopZoneRoutine();
        OnPlayerExit();
    }
    #endregion

    #region 유틸
    /// <summary>코루틴 중지 + player 참조 해제</summary>
    protected void StopZoneRoutine()
    {
        if (activeRoutine != null) { StopCoroutine(activeRoutine); activeRoutine = null; }
        player = null;
    }
    #endregion

    #region 추상 / 가상 메서드
    /// <summary>플레이어가 존 안에 있는 동안 반복 실행 (상속 클래스에서 구현)</summary>
    protected abstract IEnumerator ZoneRoutine();

    /// <summary>플레이어 진입 직후 호출 (선택 오버라이드)</summary>
    protected virtual void OnPlayerEnter(PlayerController p) { }

    /// <summary>플레이어 퇴장 직후 호출 (선택 오버라이드)</summary>
    protected virtual void OnPlayerExit() { }
    #endregion
}
