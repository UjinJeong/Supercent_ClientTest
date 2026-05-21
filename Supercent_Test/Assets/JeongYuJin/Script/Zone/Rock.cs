using UnityEngine;
using System.Collections;

/// <summary>
/// 채굴 가능한 바위
/// 체력 0 → 파괴 → 인근 플레이어에게 묶음 지급 → 리스폰
/// </summary>
public class Rock : MonoBehaviour
{
    #region 인스펙터
    [Header("스탯")]
    public float maxHealth   = 100f;
    public float respawnTime = 5f;

    [Header("드롭")]
    public int   moneyDropAmount = 1;   // 파괴 시 지급할 묶음 수
    public float pickupRadius    = 3f;  // 플레이어 탐지 반경

    [Header("사운드")]
    public AudioClip hitSFX;
    public float     hitSFXVolume   = 1f;
    public AudioClip breakSFX;
    public float     breakSFXVolume = 1f;
    #endregion

    #region 내부 변수
    private float        currentHealth;
    private bool         isDestroyed = false;
    private Collider     col;
    private MeshRenderer meshRenderer;
    #endregion

    #region 프로퍼티
    public bool IsDestroyed => isDestroyed;
    #endregion

    #region 생명주기
    void Awake()
    {
        col           = GetComponent<Collider>();
        meshRenderer  = GetComponentInChildren<MeshRenderer>();
        currentHealth = maxHealth;
    }
    #endregion

    #region 데미지 / 파괴
    /// <summary>PlayerController.HandleMining()에서 주기적으로 호출</summary>
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        // 체력 비율에 따라 스케일 축소 (시각 피드백)
        float t = Mathf.Clamp01(currentHealth / maxHealth);
        transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, t);

        if (hitSFX != null)
            AudioSource.PlayClipAtPoint(hitSFX, transform.position, hitSFXVolume);

        if (currentHealth <= 0f) Break();
    }

    void Break()
    {
        isDestroyed = true;
        if (col != null) col.enabled = false;

        if (breakSFX != null)
            AudioSource.PlayClipAtPoint(breakSFX, transform.position, breakSFXVolume);

        // 반경 내 가장 가까운 플레이어에게 묶음 지급 (가득 찼으면 무시)
        PlayerController nearest = null;
        float closest = float.MaxValue;
        foreach (var p in FindObjectsOfType<PlayerController>())
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d <= pickupRadius && d < closest) { closest = d; nearest = p; }
        }
        nearest?.PickupMoney(moneyDropAmount);

        if (meshRenderer != null) meshRenderer.enabled = false;
        StartCoroutine(Respawn());
    }
    #endregion

    #region 리스폰
    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        currentHealth = maxHealth;
        isDestroyed   = false;
        if (col != null)          col.enabled         = true;
        if (meshRenderer != null) meshRenderer.enabled = true;
        transform.localScale = Vector3.one;
    }
    #endregion
}
