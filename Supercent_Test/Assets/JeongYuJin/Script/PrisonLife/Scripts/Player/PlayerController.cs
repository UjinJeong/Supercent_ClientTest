using UnityEngine;

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

    [Header("Mining")]
    public float mineRange = 2f;
    public float mineDamage = 10f;
    public float mineInterval = 0.5f;
    public GameObject mineEffectPrefab;
    public Transform hammerPoint;          // 도끼/망치 위치

    // Internal
    private CharacterController cc;
    private Animator animator;
    private Joystick joystick;
    private Camera mainCam;

    private float mineTimer = 0f;
    private bool isMining = false;
    private Rock targetRock = null;

    private int carriedMoney = 0;
    private GameObject[] moneyStack;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashMine  = Animator.StringToHash("Mining");

    void Start()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        joystick = FindObjectOfType<Joystick>();
        mainCam = Camera.main;

        moneyStack = new GameObject[maxCarryMoney];
    }

    void Update()
    {
        HandleMovement();
        HandleMining();
    }

    // ────────────────────────────────────────────
    // Movement
    // ────────────────────────────────────────────
    void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (joystick != null)
            input = new Vector2(joystick.Horizontal, joystick.Vertical);

        // Keyboard fallback (editor)
        if (input.magnitude < 0.1f)
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (input.magnitude > 1f) input.Normalize();

        // Camera-relative movement (isometric)
        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight   = mainCam.transform.right;
        camForward.y = 0f; camForward.Normalize();
        camRight.y   = 0f; camRight.Normalize();

        Vector3 moveDir = (camForward * input.y + camRight * input.x);

        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Gravity
        moveDir.y = -9.81f;
        cc.Move(moveDir * moveSpeed * Time.deltaTime);

        float speed = new Vector2(input.x, input.y).magnitude;
        animator?.SetFloat(HashSpeed, speed);
    }

    // ────────────────────────────────────────────
    // Mining
    // ────────────────────────────────────────────
    void HandleMining()
    {
        // Find nearest rock
        Collider[] hits = Physics.OverlapSphere(transform.position, mineRange, LayerMask.GetMask("Rock"));
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
        animator?.SetBool(HashMine, isMining);

        if (!isMining) { mineTimer = 0f; return; }

        mineTimer += Time.deltaTime;
        if (mineTimer >= mineInterval)
        {
            mineTimer = 0f;
            targetRock.TakeDamage(mineDamage);

            if (mineEffectPrefab)
                Instantiate(mineEffectPrefab, targetRock.transform.position, Quaternion.identity);
        }
    }

    // ────────────────────────────────────────────
    // Money Stack
    // ────────────────────────────────────────────
    public bool PickupMoney(int amount)
    {
        if (carriedMoney >= maxCarryMoney) return false;

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
        }

        if (moneyBundlePrefab == null || moneyStackPoint == null) return;

        for (int i = 0; i < carriedMoney; i++)
        {
            Vector3 pos = moneyStackPoint.position + Vector3.up * i * 0.05f;
            moneyStack[i] = Instantiate(moneyBundlePrefab, pos, Quaternion.identity, moneyStackPoint);
        }
    }

    public int CarriedMoney => carriedMoney;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, mineRange);
    }
}
