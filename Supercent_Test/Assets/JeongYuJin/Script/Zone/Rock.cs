using UnityEngine;
using System.Collections;

/// <summary>
/// 채굴 가능한 바위 오브젝트
/// - 플레이어가 범위 내에 있으면 PlayerController.HandleMining()에서 TakeDamage() 호출
/// - 체력 0 → 파괴 → 플레이어 스택에 묶음 지급 → 일정 시간 후 리스폰
/// </summary>
public class Rock : MonoBehaviour
{
    // ── 인스펙터 ─────────────────────────────────────
    [Header("스탯")]
    public float maxHealth   = 100f;    // 최대 체력
    public float respawnTime = 5f;      // 파괴 후 리스폰까지 대기 시간(초)

    [Header("드롭")]
    public int   moneyDropAmount = 1;   // 파괴 시 플레이어 스택에 추가할 묶음 수
    public float pickupRadius    = 3f;  // 묶음을 지급할 플레이어 탐지 반경

    [Header("사운드")]
    public AudioClip hitSFX;            // 타격 효과음
    public float     hitSFXVolume  = 1f;
    public AudioClip breakSFX;          // 파괴 효과음
    public float     breakSFXVolume = 1f;

    // ── 내부 변수 ────────────────────────────────────
    private float        currentHealth;
    private bool         isDestroyed  = false;
    private Collider     col;
    private MeshRenderer meshRenderer;

    // ── 공개 프로퍼티 ────────────────────────────────
    /// <summary>현재 파괴 상태 여부 (리스폰 대기 중이면 true)</summary>
    public bool IsDestroyed => isDestroyed;

    // ── 유니티 생명주기 ──────────────────────────────
    void Awake()
    {
        col           = GetComponent<Collider>();
        meshRenderer  = GetComponentInChildren<MeshRenderer>();
        currentHealth = maxHealth;
    }

    // ── 공개 메서드 ──────────────────────────────────
    /// <summary>외부(PlayerController)에서 호출하는 데미지 처리</summary>
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        // 체력 비율에 따라 스케일 감소 (시각 피드백)
        float normalized = Mathf.Clamp01(currentHealth / maxHealth);
        transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, normalized);

        if (hitSFX != null)
            AudioSource.PlayClipAtPoint(hitSFX, transform.position, hitSFXVolume);

        if (currentHealth <= 0f)
            Break();
    }

    // ── 내부 메서드 ──────────────────────────────────
    /// <summary>체력 0 도달 시 파괴 처리 및 묶음 지급</summary>
    void Break()
    {
        isDestroyed = true;
        if (col != null) col.enabled = false;

        if (breakSFX != null)
            AudioSource.PlayClipAtPoint(breakSFX, transform.position, breakSFXVolume);

        // 반경 내 가장 가까운 플레이어에게 묶음 지급
        // 플레이어가 없거나 가득 찼으면 그냥 무시
        PlayerController nearestPlayer = null;
        float closest = float.MaxValue;
        foreach (var p in FindObjectsOfType<PlayerController>())
        {
            if (p == null) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d <= pickupRadius && d < closest) { closest = d; nearestPlayer = p; }
        }

        if (nearestPlayer != null)
            nearestPlayer.PickupMoney(moneyDropAmount);

        if (meshRenderer != null) meshRenderer.enabled = false;
        StartCoroutine(Respawn());
    }

    /// <summary>일정 시간 후 바위를 원래 상태로 복구</summary>
    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        currentHealth = maxHealth;
        isDestroyed   = false;
        if (col != null)          col.enabled          = true;
        if (meshRenderer != null) meshRenderer.enabled  = true;
        transform.localScale = Vector3.one;
    }
}
