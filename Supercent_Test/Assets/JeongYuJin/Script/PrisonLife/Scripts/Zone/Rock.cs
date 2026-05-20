using UnityEngine;
using System.Collections;

public class Rock : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float respawnTime = 5f;

    [Header("Drop")]
    public int moneyDropAmount = 1;

    [Header("VFX")]
    public GameObject breakParticlePrefab;
    public GameObject[] fragments;          // 부서진 파편 조각들

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

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        // 스케일로 데미지 피드백
        float scale = Mathf.Lerp(0.7f, 1f, currentHealth / maxHealth);
        transform.localScale = Vector3.one * scale;

        if (currentHealth <= 0f)
            Break();
    }

    void Break()
    {
        isDestroyed = true;
        col.enabled = false;

        if (breakParticlePrefab)
            Instantiate(breakParticlePrefab, transform.position, Quaternion.identity);

        // 파편 날리기
        foreach (var frag in fragments)
        {
            if (frag == null) continue;
            frag.SetActive(true);
            Rigidbody rb = frag.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddExplosionForce(200f, transform.position, 3f);
        }

        // 돈 드롭 (근처에 있는 플레이어에게)
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
            player.PickupMoney(moneyDropAmount);

        meshRenderer.enabled = false;

        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        // 파편 정리
        foreach (var frag in fragments)
            if (frag != null) frag.SetActive(false);

        currentHealth = maxHealth;
        isDestroyed   = false;
        col.enabled   = true;
        meshRenderer.enabled = true;
        transform.localScale = Vector3.one;
    }
}
