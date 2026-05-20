using UnityEngine;
using System.Collections;

public class Rock : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float respawnTime = 5f;

    [Header("Drop")]
    // 떨어뜨릴 돈 묶음 수 (Player.PickupMoney의 인자)
    public int moneyDropAmount = 1;
    // 플레이어에게 줄 최대 탐지 반경 (근처 플레이어에게 우선 지급)
    public float pickupRadius = 3f;

    [Header("Sound")]
    // 데미지를 받을 때 재생될 효과음 (한 번의 타격마다 재생)
    public AudioClip hitSFX;
    public float hitSFXVolume = 1f;
    // 부서질 때 재생될 효과음 (선택)
    public AudioClip breakSFX;
    public float breakSFXVolume = 1f;

    private float currentHealth;
    private bool isDestroyed = false;
    private Collider col;
    private MeshRenderer meshRenderer;

    public bool IsDestroyed => isDestroyed;

    void Awake()
    {
        col          = GetComponent<Collider>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        currentHealth = maxHealth;
    }

    // 데미지 처리: damage는 실제 체력 차감값 (기존 설계 유지)
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        // 시각적 피드백으로 스케일 변화 (0..1 범위로 보정)
        float normalized = Mathf.Clamp01(currentHealth / maxHealth);
        float scale = Mathf.Lerp(0.7f, 1f, normalized);
        transform.localScale = Vector3.one * scale;

        // 데미지 시 효과음 재생
        if (hitSFX != null)
            AudioSource.PlayClipAtPoint(hitSFX, transform.position, hitSFXVolume);

        if (currentHealth <= 0f)
            Break();
    }

    // 파괴 처리: 근처 플레이어 우선 지급, 없거나 가득 찼으면 직접 GameManager에 추가
    void Break()
    {
        isDestroyed = true;
        if (col != null) col.enabled = false;

        // 부서질 때 효과음 재생 (있다면)
        if (breakSFX != null)
            AudioSource.PlayClipAtPoint(breakSFX, transform.position, breakSFXVolume);

        // 1) 가까운 플레이어(들) 중 가장 가까운 것 탐색
        PlayerController nearestPlayer = null;
        float closest = float.MaxValue;
        var players = FindObjectsOfType<PlayerController>();
        foreach (var p in players)
        {
            if (p == null) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d <= pickupRadius && d < closest)
            {
                closest = d;
                nearestPlayer = p;
            }
        }

        bool handedToPlayer = false;

        // 2) 플레이어가 있으면 PickupMoney 시도
        if (nearestPlayer != null)
        {
            // PickupMoney는 성공/실패(true/false)를 반환하므로 확인
            handedToPlayer = nearestPlayer.PickupMoney(moneyDropAmount);
            // 시각 피드백(팝업)은 PlayerPickup 쪽에서 처리될 수 있으나, 안전하게 처리
            if (handedToPlayer)
            {
                // 월드 팝업: 실제 금액 단위(예: 묶음당 10이라면 *10). 
                // UIManager가 존재하면 보여줌. (없으면 무시)
                UIManager.Instance?.ShowMoneyPopup(transform.position + Vector3.up * 1f, moneyDropAmount * 10);
            }
        }

        // 3) 플레이어가 없거나 플레이어 인벤토리가 가득 차서 Pickup이 실패하면 바로 GameManager에 추가
        if (!handedToPlayer)
        {
            // GameManager가 있으면 즉시 금액 추가(묶음당 10 단위 가정)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMoney(moneyDropAmount * 10);
                UIManager.Instance?.ShowMoneyPopup(transform.position + Vector3.up * 1f, moneyDropAmount * 10);
            }
        }

        // 본체 렌더러 숨기기
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        currentHealth = maxHealth;
        isDestroyed   = false;
        if (col != null) col.enabled   = true;
        if (meshRenderer != null) meshRenderer.enabled = true;
        transform.localScale = Vector3.one;
    }
}
