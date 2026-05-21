using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어 이동, 채굴, 돌 스택, 수갑 스택, MAX 인디케이터를 담당
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ── 인스펙터 ─────────────────────────────────────
    [Header("이동")]
    public float moveSpeed     = 5f;    // 이동 속도
    public float rotationSpeed = 10f;   // 회전 보간 속도

    [Header("돌 스택")]
    public Transform  moneyStackPoint;          // 플레이어 등 위의 스택 기준점
    public GameObject moneyBundlePrefab;        // 돌 묶음 프리팹
    public int        maxCarryMoney      = 20;  // 최대 보유 가능 묶음 수
    public float      moneyStackVerticalSpacing = 0.05f; // 묶음 간 기본 세로 간격

    [Header("수갑 스택")]
    public Transform  handcuffStackPoint;                    // 수갑 쌓기 기준점 (비우면 돌 스택 포인트 사용)
    public GameObject handcuffBundlePrefab;                  // 수갑 프리팹
    public int        maxCarryHandcuffs           = 20;      // 최대 보유 가능 수갑 수
    public float      handcuffStackVerticalSpacing = 0.1f;   // 수갑 간 세로 간격

    [Header("MAX 인디케이터")]
    public GameObject maxIndicatorPrefab;                                  // MAX 표시 프리팹
    public Vector3    maxIndicatorLocalOffset  = new Vector3(0f, 2f, 0f); // 머리 위 오프셋
    public float      maxIndicatorDuration     = 0.8f;                    // 표시 지속 시간(초)
    public float      maxIndicatorRiseDistance = 0.6f;                    // 위로 떠오르는 거리

    [Header("채굴")]
    public float     mineRange    = 2f;     // 채굴 감지 반경
    public float     mineDamage   = 10f;    // 1회 타격 데미지
    public float     mineInterval = 0.5f;   // 타격 주기(초)
    public LayerMask rockLayerMask;         // 바위 레이어 마스크

    // ── 내부 변수 ────────────────────────────────────
    private CharacterController cc;
    private Camera mainCam;

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

    // ── 유니티 생명주기 ──────────────────────────────
    void Start()
    {
        cc      = GetComponent<CharacterController>();
        mainCam = Camera.main;

        // 스택 배열 초기화
        moneyStack    = new GameObject[maxCarryMoney];
        handcuffStack = new GameObject[maxCarryHandcuffs];

        // MAX 인디케이터 프리팹을 플레이어 자식으로 생성 후 비활성화
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
        // MAX 인디케이터가 활성화 중일 때 항상 카메라를 향하도록 회전
        if (maxIndicatorInstance != null && maxIndicatorInstance.activeSelf && mainCam != null)
        {
            Vector3 dir = maxIndicatorInstance.transform.position - mainCam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                maxIndicatorInstance.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    // ── 이동 ─────────────────────────────────────────
    void HandleMovement()
    {
        // 조이스틱 우선, 없으면 키보드 입력 사용
        Vector2 input = Vector2.zero;
        if (UIManager.Instance != null)
            input = new Vector2(UIManager.Instance.Horizontal, UIManager.Instance.Vertical);
        if (input.magnitude < 0.1f)
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (input.magnitude > 1f) input.Normalize();

        // 카메라 방향 기준으로 이동 방향 계산 (y축 제거)
        Vector3 camForward = mainCam.transform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight   = mainCam.transform.right;   camRight.y   = 0f; camRight.Normalize();
        Vector3 moveDir    = camForward * input.y + camRight * input.x;

        // 이동 방향이 있을 때만 회전 보간
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // 중력 적용 후 이동
        moveDir.y = -9.81f;
        cc.Move(moveDir * moveSpeed * Time.deltaTime);
    }

    // ── 채굴 ─────────────────────────────────────────
    void HandleMining()
    {
        // 주변 바위 탐색 (레이어 마스크 미설정 시 전체 탐색)
        Collider[] hits = rockLayerMask != 0
            ? Physics.OverlapSphere(transform.position, mineRange, rockLayerMask)
            : Physics.OverlapSphere(transform.position, mineRange);

        // 가장 가까운 바위를 타겟으로 선정
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

        // 타격 주기마다 데미지 적용
        mineTimer += Time.deltaTime;
        if (mineTimer >= mineInterval)
        {
            mineTimer = 0f;
            targetRock.TakeDamage(mineDamage);
        }
    }

    // ── 돌 스택 ──────────────────────────────────────
    /// <summary>
    /// 돌 묶음을 amount개 소비한다. (HandcuffZone에서 사용)
    /// </summary>
    /// <returns>성공 시 true, 보유량 부족 시 false</returns>
    public bool ConsumeRock(int amount)
    {
        if (carriedMoney < amount) return false;
        carriedMoney -= amount;
        RefreshMoneyStack();
        return true;
    }

    // ── 수갑 스택 ─────────────────────────────────────
    /// <summary>
    /// 수갑을 amount개 플레이어 등에 추가한다.
    /// </summary>
    /// <returns>성공 시 true, 가득 찼으면 false</returns>
    public bool AddHandcuffToCarry(int amount)
    {
        if (carriedHandcuffs >= maxCarryHandcuffs) return false;
        carriedHandcuffs = Mathf.Min(carriedHandcuffs + amount, maxCarryHandcuffs);
        RefreshHandcuffStack();
        return true;
    }

    /// <summary>수갑을 amount개 소비한다. (배포 존에서 사용)</summary>
    /// <returns>성공 시 true, 보유량 부족 시 false</returns>
    public bool ConsumeHandcuff(int amount)
    {
        if (carriedHandcuffs < amount) return false;
        carriedHandcuffs -= amount;
        RefreshHandcuffStack();
        return true;
    }

    /// <summary>현재 보유 수갑 수</summary>
    public int CarriedHandcuffs => carriedHandcuffs;

    /// <summary>등에 쌓인 수갑 오브젝트를 현재 상태에 맞게 재생성</summary>
    void RefreshHandcuffStack()
    {
        // 기존 수갑 오브젝트 전부 제거
        for (int i = 0; i < maxCarryHandcuffs; i++)
        {
            if (handcuffStack[i] != null) Destroy(handcuffStack[i]);
            handcuffStack[i] = null;
        }

        if (handcuffBundlePrefab == null) return;

        // 기준점: handcuffStackPoint가 없으면 moneyStackPoint 사용
        Transform stackRoot = handcuffStackPoint != null ? handcuffStackPoint : moneyStackPoint;
        if (stackRoot == null) return;

        float spacing = Mathf.Max(0.01f, handcuffStackVerticalSpacing);

        // 프리팹 실제 높이로 간격 보정
        var prefabRenderer = handcuffBundlePrefab.GetComponentInChildren<Renderer>();
        if (prefabRenderer != null)
        {
            float h = prefabRenderer.bounds.size.y;
            if (h > 0f) spacing = Mathf.Max(spacing, h * 0.9f);
        }

        // 아래에서 위로 수갑 쌓기
        for (int i = 0; i < carriedHandcuffs; i++)
        {
            GameObject go = Instantiate(handcuffBundlePrefab, stackRoot);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = new Vector3(0f, spacing * i, 0f);
            handcuffStack[i] = go;
        }
    }

    /// <summary>
    /// 돌 묶음을 등에 추가한다.
    /// </summary>
    /// <returns>성공 시 true, 이미 가득 찼으면 false</returns>
    public bool PickupMoney(int amount)
    {
        if (carriedMoney >= maxCarryMoney)
        {
            PlayMaxIndicatorOnce(); // 가득 찼음을 시각적으로 알림
            return false;
        }
        carriedMoney = Mathf.Min(carriedMoney + amount, maxCarryMoney);
        RefreshMoneyStack();
        return true;
    }

    /// <summary>
    /// 보유한 돌 묶음 전량을 입금하고 GameManager에 반영한다.
    /// </summary>
    /// <returns>입금한 묶음 수</returns>
    public int DepositMoney()
    {
        int deposited = carriedMoney;
        carriedMoney  = 0;
        RefreshMoneyStack();
        GameManager.Instance.AddMoney(deposited * 10); // 묶음 1개 = 10원
        return deposited;
    }

    /// <summary>현재 보유 돌 묶음 수</summary>
    public int CarriedMoney => carriedMoney;

    /// <summary>등에 쌓인 돌 묶음 오브젝트를 현재 상태에 맞게 재생성</summary>
    void RefreshMoneyStack()
    {
        // 기존 묶음 전부 제거
        for (int i = 0; i < maxCarryMoney; i++)
        {
            if (moneyStack[i] != null) Destroy(moneyStack[i]);
            moneyStack[i] = null;
        }

        if (moneyBundlePrefab == null || moneyStackPoint == null) return;

        // 프리팹 실제 높이를 간격으로 사용 (설정값보다 크면 높이 우선)
        float spacing = Mathf.Max(0.01f, moneyStackVerticalSpacing);
        var prefabRenderer = moneyBundlePrefab.GetComponentInChildren<Renderer>();
        if (prefabRenderer != null)
        {
            float h = prefabRenderer.bounds.size.y;
            if (h > 0f) spacing = Mathf.Max(spacing, h * 0.9f);
        }

        // 보유 개수만큼 아래에서 위로 쌓기
        for (int i = 0; i < carriedMoney; i++)
        {
            GameObject go = Instantiate(moneyBundlePrefab, moneyStackPoint);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;
            go.transform.localPosition = new Vector3(0f, spacing * i, 0f);
            moneyStack[i] = go;
        }
    }

    // ── MAX 인디케이터 ───────────────────────────────
    /// <summary>MAX 인디케이터 애니메이션을 (재)시작한다</summary>
    public void PlayMaxIndicatorOnce()
    {
        if (maxIndicatorInstance == null) return;

        // 이미 재생 중이면 중단 후 재시작
        if (maxIndicatorCoroutine != null)
        {
            StopCoroutine(maxIndicatorCoroutine);
            maxIndicatorCoroutine = null;
        }
        maxIndicatorCoroutine = StartCoroutine(AnimateMaxIndicator());
    }

    /// <summary>위로 떠오르면서 페이드 아웃되는 MAX 인디케이터 애니메이션</summary>
    IEnumerator AnimateMaxIndicator()
    {
        maxIndicatorInstance.SetActive(true);
        maxIndicatorInstance.transform.localPosition = maxIndicatorLocalOffset;
        SetIndicatorAlpha(1f);

        float   elapsed = 0f;
        Vector3 from    = maxIndicatorLocalOffset;
        Vector3 to      = maxIndicatorLocalOffset + Vector3.up * maxIndicatorRiseDistance;

        // 알파를 제어할 컴포넌트 참조 캐시
        CanvasGroup cg    = maxIndicatorInstance.GetComponent<CanvasGroup>();
        TMP_Text    tmp   = maxIndicatorInstance.GetComponentInChildren<TMP_Text>(true);
        Renderer[]  rends = maxIndicatorInstance.GetComponentsInChildren<Renderer>(true);

        while (elapsed < maxIndicatorDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / maxIndicatorDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float alpha = 1f - t; // 시간이 지날수록 투명해짐

            // 위치 보간
            maxIndicatorInstance.transform.localPosition = Vector3.LerpUnclamped(from, to, eased);

            // 알파 적용 (CanvasGroup / TMP_Text / Renderer 순서로 처리)
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

    /// <summary>인디케이터의 모든 알파값을 지정값으로 초기화</summary>
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
}
