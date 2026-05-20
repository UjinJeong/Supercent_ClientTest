using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Money Stack")]
    public Transform moneyStackPoint;      // 플레이어 앞에 돈 쌓이는 위치
    public GameObject moneyBundlePrefab;
    public int maxCarryMoney = 20;         // 최대 들고 다닐 수 있는 돈 묶음 수
    public float moneyStackVerticalSpacing = 0.05f;

    [Header("Max Indicator")]
    public GameObject maxIndicatorPrefab;                // 머리 위 표시 프리팹
    public Vector3 maxIndicatorLocalOffset = new Vector3(0f, 2.0f, 0f);
    public float maxIndicatorDuration = 0.8f;            // 올라갔다 사라지는 시간
    public float maxIndicatorRiseDistance = 0.6f;        // 올라가는 거리

    [Header("Mining")]
    public float mineRange = 2f;
    public float mineDamage = 10f;
    public float mineInterval = 0.5f;

    public LayerMask rockLayerMask;
    public bool debugMining = false;

    // Internal
    private CharacterController cc;
    private Joystick joystick;
    private Camera mainCam;

    private float mineTimer = 0f;
    private bool isMining = false;
    private Rock targetRock = null;

    private int carriedMoney = 0;
    private GameObject[] moneyStack;

    // Max indicator 인스턴스 및 코루틴 참조 (단발 재생용)
    private GameObject maxIndicatorInstance;
    private Coroutine maxIndicatorCoroutine;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        joystick = FindObjectOfType<Joystick>();
        mainCam = Camera.main;

        moneyStack = new GameObject[maxCarryMoney];

        // Max indicator 프리팹을 자식으로 생성해두고 비활성화
        if (maxIndicatorPrefab != null)
        {
            maxIndicatorInstance = Instantiate(maxIndicatorPrefab, transform);
            maxIndicatorInstance.transform.localPosition = maxIndicatorLocalOffset;
            maxIndicatorInstance.transform.localRotation = Quaternion.identity;
            maxIndicatorInstance.SetActive(false);
        }
    }

    void Update()
    {
        HandleMovement();
        HandleMining();
    }

    void LateUpdate()
    {
        // 인디케이터가 활성화된 동안 항상 카메라를 바라보게 함
        if (maxIndicatorInstance != null && maxIndicatorInstance.activeSelf && mainCam != null)
        {
            Vector3 dir = maxIndicatorInstance.transform.position - mainCam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                maxIndicatorInstance.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        if (joystick != null) input = new Vector2(joystick.Horizontal, joystick.Vertical);
        if (input.magnitude < 0.1f) input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (input.magnitude > 1f) input.Normalize();

        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;
        camForward.y = 0f; camForward.Normalize();
        camRight.y = 0f; camRight.Normalize();

        Vector3 moveDir = (camForward * input.y + camRight * input.x);

        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        moveDir.y = -9.81f;
        cc.Move(moveDir * moveSpeed * Time.deltaTime);
    }

    void HandleMining()
    {
        Collider[] hits;
        if (rockLayerMask != 0) hits = Physics.OverlapSphere(transform.position, mineRange, rockLayerMask);
        else hits = Physics.OverlapSphere(transform.position, mineRange);

        targetRock = null;
        float closest = float.MaxValue;

        foreach (var h in hits)
        {
            Rock r = h.GetComponent<Rock>();
            if (r == null || r.IsDestroyed) continue;
            float dist = Vector3.Distance(transform.position, h.transform.position);
            if (dist < closest) { closest = dist; targetRock = r; }
        }

        isMining = targetRock != null;

        if (!isMining) { mineTimer = 0f; return; }

        mineTimer += Time.deltaTime;
        if (mineTimer >= mineInterval)
        {
            mineTimer = 0f;
            targetRock.TakeDamage(mineDamage);
        }

        if (debugMining)
            Debug.DrawLine(transform.position, targetRock != null ? targetRock.transform.position : transform.position, Color.red);
    }

    // Money stack 관련
    // 반환값: 성공(true) / 실패(가득참 -> false)
    public bool PickupMoney(int amount)
    {
        if (carriedMoney >= maxCarryMoney)
        {
            // 가득 찼을 때 단발로 max 인디케이터 재생
            PlayMaxIndicatorOnce();
            return false;
        }

        carriedMoney = Mathf.Min(carriedMoney + amount, maxCarryMoney);
        RefreshMoneyStack();
        return true;
    }

    public int DepositMoney()
    {
        int deposited = carriedMoney;
        carriedMoney = 0;
        RefreshMoneyStack();
        GameManager.Instance.AddMoney(deposited * 10);
        return deposited;
    }

    void RefreshMoneyStack()
    {
        for (int i = 0; i < maxCarryMoney; i++)
        {
            if (moneyStack[i] != null) Destroy(moneyStack[i]);
            moneyStack[i] = null;
        }

        if (moneyBundlePrefab == null || moneyStackPoint == null) return;

        float spacing = Mathf.Max(0.01f, moneyStackVerticalSpacing);
        var prefabRenderer = moneyBundlePrefab.GetComponentInChildren<Renderer>();
        if (prefabRenderer != null)
        {
            float prefabHeight = prefabRenderer.bounds.size.y;
            if (prefabHeight > 0f) spacing = Mathf.Max(spacing, prefabHeight * 0.9f);
        }

        for (int i = 0; i < carriedMoney; i++)
        {
            GameObject go = Instantiate(moneyBundlePrefab, moneyStackPoint);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = new Vector3(0f, spacing * i, 0f);
            moneyStack[i] = go;
        }
    }

    // 외부(예: Rock)에서 호출하는 단발 인디케이터 재생 API
    public void PlayMaxIndicatorOnce()
    {
        if (maxIndicatorInstance == null) return;

        // 이미 단발 애니메이션이 진행 중이면 재시작
        if (maxIndicatorCoroutine != null)
        {
            StopCoroutine(maxIndicatorCoroutine);
            maxIndicatorCoroutine = null;
        }

        maxIndicatorCoroutine = StartCoroutine(AnimateMaxIndicator());
    }

    // 단일 애니메이션: 올라가며 페이드 후 종료 (한 사이클)
    IEnumerator AnimateMaxIndicator()
    {
        maxIndicatorInstance.SetActive(true);
        maxIndicatorInstance.transform.localPosition = maxIndicatorLocalOffset;
        ResetIndicatorAlpha();

        float elapsed = 0f;
        Vector3 from = maxIndicatorLocalOffset;
        Vector3 to = maxIndicatorLocalOffset + Vector3.up * maxIndicatorRiseDistance;

        CanvasGroup cg = maxIndicatorInstance.GetComponent<CanvasGroup>();
        TMP_Text tmp = maxIndicatorInstance.GetComponentInChildren<TMP_Text>(true);
        Renderer[] rends = maxIndicatorInstance.GetComponentsInChildren<Renderer>(true);

        while (elapsed < maxIndicatorDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / maxIndicatorDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            maxIndicatorInstance.transform.localPosition = Vector3.LerpUnclamped(from, to, eased);

            float alpha = 1f - t;
            if (cg != null) cg.alpha = alpha;
            if (tmp != null) { Color c = tmp.color; c.a = alpha; tmp.color = c; }
            if (rends != null)
            {
                foreach (var r in rends)
                {
                    foreach (var mat in r.materials)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color col = mat.color;
                            col.a = alpha;
                            mat.color = col;
                        }
                    }
                }
            }

            yield return null;
        }

        maxIndicatorInstance.SetActive(false);
        maxIndicatorInstance.transform.localPosition = maxIndicatorLocalOffset;
        ResetIndicatorAlpha();
        maxIndicatorCoroutine = null;
    }

    void ResetIndicatorAlpha()
    {
        if (maxIndicatorInstance == null) return;
        CanvasGroup cg = maxIndicatorInstance.GetComponent<CanvasGroup>();
        TMP_Text tmp = maxIndicatorInstance.GetComponentInChildren<TMP_Text>(true);
        Renderer[] rends = maxIndicatorInstance.GetComponentsInChildren<Renderer>(true);

        if (cg != null) cg.alpha = 1f;
        if (tmp != null) { Color c = tmp.color; c.a = 1f; tmp.color = c; }
        if (rends != null)
        {
            foreach (var r in rends)
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color col = mat.color;
                        col.a = 1f;
                        mat.color = col;
                    }
                }
            }
        }
    }

    public int CarriedMoney => carriedMoney;
}
