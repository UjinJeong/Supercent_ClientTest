using UnityEngine;
using System.Collections;

public class Rock : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float respawnTime = 5f;

    [Header("Drop")]
    // 바위 파괴 시 플레이어 스택에 추가할 묶음 수
    public int moneyDropAmount = 1;
    // 근처 플레이어 탐지 반경
    public float pickupRadius = 3f;

    [Header("Sound")]
    public AudioClip hitSFX;
    public float hitSFXVolume = 1f;
    public AudioClip breakSFX;
    public float breakSFXVolume = 1f;

    private float currentHealth;
    private bool isDestroyed = false;
    private Collider col;
    private MeshRenderer meshRenderer;

    public bool IsDestroyed => isDestroyed;

    void Awake()
    {
        col           = GetComponent<Collider>();
        meshRenderer  = GetComponentInChildren<MeshRenderer>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        // 체력에 따른 스케일 시각 피드백
        float normalized = Mathf.Clamp01(currentHealth / maxHealth);
        transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, normalized);

        if (hitSFX != null)
            AudioSource.PlayClipAtPoint(hitSFX, transform.position, hitSFXVolume);

        if (currentHealth <= 0f)
            Break();
    }

    void Break()
    {
        isDestroyed = true;
        if (col != null) col.enabled = false;

        if (breakSFX != null)
            AudioSource.PlayClipAtPoint(breakSFX, transform.position, breakSFXVolume);

        // 근처 플레이어에게 묶음 지급 — 가득 찼거나 없으면 그냥 무시
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

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        currentHealth = maxHealth;
        isDestroyed   = false;
        if (col != null) col.enabled = true;
        if (meshRenderer != null) meshRenderer.enabled = true;
        transform.localScale = Vector3.one;
    }
}
