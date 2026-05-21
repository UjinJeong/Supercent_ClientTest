using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>플레이어 이동 / 채굴 / 돌 스택 / 수갑 스택 / MAX 인디케이터</summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    #region 인스펙터
    [Header("이동")]
    public float moveSpeed     = 5f;
    public float rotationSpeed = 10f;

    [Header("돌 스택")]
    public Transform  moneyStackPoint;
    public GameObject moneyBundlePrefab;
    public int        maxCarryMoney             = 20;
    public float      moneyStackVerticalSpacing = 0.05f;

    [Header("수갑 스택")]
    public Transform  handcuffStackPoint;                     // 비우면 돌 스택 포인트 공유
    public GameObject handcuffBundlePrefab;
    public int        maxCarryHandcuffs           = 20;
    public float      handcuffStackVerticalSpacing = 0.1f;

    [Header("MAX 인디케이터")]
    public GameObject maxIndicatorPrefab;
    public Vector3    maxIndicatorLocalOffset  = new Vector3(0f, 2f, 0f);
    public float      maxIndicatorDuration     = 0.8f;
    public float      maxIndicatorRiseDistance = 0.6f;

    [Header("채굴")]
    public float     mineRange    = 2f;
    public float     mineDamage   = 10f;
    public float     mineInterval = 0.5f;
    public LayerMask rockLayerMask;
    #endregion

    #region 내부 변수
    private CharacterController cc;
    private Camera              mainCam;

    // 채굴
    private float mineTimer  = 0f;
    private Rock  targetRock = null;

    // 돌 스택
    private int          carriedMoney = 0;
    private GameObject[] moneyStack;

    // 수갑 스택
    private int          carriedHandcuffs = 0;
    private GameObject[] handcuffStack;

    // MAX 인디케이터
    private GameObject maxIndicatorInstance;
    private Coroutine  maxIndicatorCoroutine;
    #endregion

    #region 생명주기
    void Start()
    {
        cc      = GetComponent<CharacterController>();
        mainCam = Camera.main;

        moneyStack    = new GameObject[maxCarryMoney];
        handcuffStack = new GameObject[maxCarryHandcuffs];

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
        // MAX 인디케이터가 항상 카메라를 바라보도록 회전
        if (maxIndicatorInstance != null && maxIndicatorInstance.activeSelf && mainCam != null)
        {
            Vector3 dir = maxIndicatorInstance.transform.position - mainCam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                maxIndicatorInstance.transform.rotation = Quaternion.LookRotation(dir);
        }
    }
    #endregion

    #region 이동
    void HandleMovement()
    {
        // 조이스틱 우선, 없으면 키보드 폴백
        Vector2 input = Vector2.zero;
        if (UIManager.Instance != null)
            input = new Vector2(UIManager.Instance.Horizontal, UIManager.Instance.Vertical);
        if (input.magnitude < 0.1f)
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (input.magnitude > 1f) input.Normalize();

        // 카메라 기준 이동 방향 (y축 제거)
        Vector3 camForward = mainCam.transform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight   = mainCam.transform.right;   camRight.y   = 0f; camRight.Normalize();
        Vector3 moveDir    = camForward * input.y + camRight * input.x;

        if (moveDir.magnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                rotationSpeed * Time.deltaTime);

        moveDir.y = -9.81f;
        cc.Move(moveDir * moveSpeed * Time.deltaTime);
    }
    #endregion

    #region 채굴
    void HandleMining()
    {
        Collider[] hits = rockLayerMask != 0
            ? Physics.OverlapSphere(transform.position, mineRange, rockLayerMask)
            : Physics.OverlapSphere(transform.position, mineRange);

        targetRock = null;
        float closest = float.MaxValue;
        foreach (var h in hits)
        {
            Rock r = h.GetComponent<Rock>();
            if (r == null || r.IsDestroyed) continue;
            float dist = Vector3.Distance(transform.position, h.transform.position);
            if (dist < closest) { closest = dist; targetRock = r; }
        }

        if (targetRock == null) { mineTimer = 0f; return; }

        mineTimer += Time.deltaTime;
        if (mineTimer >= mineInterval)
        {
            mineTimer = 0f;
            targetRock.TakeDamage(mineDamage);
        }
    }
    #endregion

    #region 돌 스택
    public int  CarriedMoney => carriedMoney;

    /// <summary>돌 묶음 추가 — 가득 찼으면 MAX 인디케이터 표시 후 false 반환</summary>
    public bool PickupMoney(int amount)
    {
        if (carriedMoney >= maxCarryMoney) { PlayMaxIndicatorOnce(); return false; }
        carriedMoney = Mathf.Min(carriedMoney + amount, maxCarryMoney);
        RefreshMoneyStack();
        return true;
    }

    /// <summary>보유 돌 amount개 소비 (HandcuffZone 사용)</summary>
    public bool ConsumeRock(int amount)
    {
        if (carriedMoney < amount) return false;
        carriedMoney -= amount;
        RefreshMoneyStack();
        return true;
    }

    /// <summary>보유 돌 전량 입금 → GameManager에 반영</summary>
    public int DepositMoney()
    {
        int deposited = carriedMoney;
        carriedMoney  = 0;
        RefreshMoneyStack();
        GameManager.Instance.AddMoney(deposited * 10); // 묶음 1개 = 10원
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

        float spacing = CalcSpacing(moneyBundlePrefab, moneyStackVerticalSpacing);
        for (int i = 0; i < carriedMoney; i++)
        {
            GameObject go = Instantiate(moneyBundlePrefab, moneyStackPoint);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;
            go.transform.localPosition = new Vector3(0f, spacing * i, 0f);
            moneyStack[i] = go;
        }
    }
    #endregion

    #region 수갑 스택
    public int CarriedHandcuffs => carriedHandcuffs;

    /// <summary>수갑 추가 — 가득 찼으면 false 반환</summary>
    public bool AddHandcuffToCarry(int amount)
    {
        if (carriedHandcuffs >= maxCarryHandcuffs) return false;
        carriedHandcuffs = Mathf.Min(carriedHandcuffs + amount, maxCarryHandcuffs);
        RefreshHandcuffStack();
        return true;
    }

    /// <summary>수갑 amount개 소비 (배포 존 사용)</summary>
    public bool ConsumeHandcuff(int amount)
    {
        if (carriedHandcuffs < amount) return false;
        carriedHandcuffs -= amount;
        RefreshHandcuffStack();
        return true;
    }

    void RefreshHandcuffStack()
    {
        for (int i = 0; i < maxCarryHandcuffs; i++)
        {
            if (handcuffStack[i] != null) Destroy(handcuffStack[i]);
            handcuffStack[i] = null;
        }

        if (handcuffBundlePrefab == null) return;

        Transform stackRoot = handcuffStackPoint != null ? handcuffStackPoint : moneyStackPoint;
        if (stackRoot == null) return;

        float spacing = CalcSpacing(handcuffBundlePrefab, handcuffStackVerticalSpacing);
        for (int i = 0; i < carriedHandcuffs; i++)
        {
            GameObject go = Instantiate(handcuffBundlePrefab, stackRoot);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = new Vector3(0f, spacing * i, 0f);
            handcuffStack[i] = go;
        }
    }

    /// <summary>프리팹 실제 높이를 반영한 스택 간격 계산</summary>
    float CalcSpacing(GameObject prefab, float baseSpacing)
    {
        float spacing = Mathf.Max(0.01f, baseSpacing);
        var r = prefab.GetComponentInChildren<Renderer>();
        if (r != null && r.bounds.size.y > 0f)
            spacing = Mathf.Max(spacing, r.bounds.size.y * 0.9f);
        return spacing;
    }
    #endregion

    #region MAX 인디케이터
    public void PlayMaxIndicatorOnce()
    {
        if (maxIndicatorInstance == null) return;
        if (maxIndicatorCoroutine != null) { StopCoroutine(maxIndicatorCoroutine); maxIndicatorCoroutine = null; }
        maxIndicatorCoroutine = StartCoroutine(AnimateMaxIndicator());
    }

    IEnumerator AnimateMaxIndicator()
    {
        maxIndicatorInstance.SetActive(true);
        maxIndicatorInstance.transform.localPosition = maxIndicatorLocalOffset;
        SetIndicatorAlpha(1f);

        float   elapsed = 0f;
        Vector3 from    = maxIndicatorLocalOffset;
        Vector3 to      = maxIndicatorLocalOffset + Vector3.up * maxIndicatorRiseDistance;

        CanvasGroup cg    = maxIndicatorInstance.GetComponent<CanvasGroup>();
        TMP_Text    tmp   = maxIndicatorInstance.GetComponentInChildren<TMP_Text>(true);
        Renderer[]  rends = maxIndicatorInstance.GetComponentsInChildren<Renderer>(true);

        while (elapsed < maxIndicatorDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / maxIndicatorDuration);
            float alpha = 1f - t;

            maxIndicatorInstance.transform.localPosition =
                Vector3.LerpUnclamped(from, to, Mathf.SmoothStep(0f, 1f, t));

            if (cg  != null) cg.alpha = alpha;
            if (tmp != null) { Color c = tmp.color; c.a = alpha; tmp.color = c; }
            foreach (var r in rends)
                foreach (var mat in r.materials)
                    if (mat.HasProperty("_Color")) { Color col = mat.color; col.a = alpha; mat.color = col; }

            yield return null;
        }

        maxIndicatorInstance.SetActive(false);
        maxIndicatorInstance.transform.localPosition = maxIndicatorLocalOffset;
        SetIndicatorAlpha(1f);
        maxIndicatorCoroutine = null;
    }

    void SetIndicatorAlpha(float alpha)
    {
        if (maxIndicatorInstance == null) return;
        CanvasGroup cg    = maxIndicatorInstance.GetComponent<CanvasGroup>();
        TMP_Text    tmp   = maxIndicatorInstance.GetComponentInChildren<TMP_Text>(true);
        Renderer[]  rends = maxIndicatorInstance.GetComponentsInChildren<Renderer>(true);

        if (cg  != null) cg.alpha = alpha;
        if (tmp != null) { Color c = tmp.color; c.a = alpha; tmp.color = c; }
        foreach (var r in rends)
            foreach (var mat in r.materials)
                if (mat.HasProperty("_Color")) { Color col = mat.color; col.a = alpha; mat.color = col; }
    }
    #endregion
}
