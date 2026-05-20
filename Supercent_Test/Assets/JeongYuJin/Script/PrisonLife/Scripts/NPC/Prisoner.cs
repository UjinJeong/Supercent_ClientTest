using UnityEngine;
using UnityEngine.AI;

public class Prisoner : MonoBehaviour
{
    public enum PrisonerState { Free, Arrested, InCell, Working }

    [Header("Stats")]
    public PrisonerState state = PrisonerState.Free;
    public float walkSpeed = 2f;
    public float workSpeed = 1.5f;

    [Header("Work")]
    public float mineInterval = 1.5f;
    public float mineDamage = 5f;

    private NavMeshAgent agent;
    private Animator animator;
    private float mineTimer;

    private Rock targetRock;
    private Transform cellPoint;

    private static readonly int HashSpeed  = Animator.StringToHash("Speed");
    private static readonly int HashWork   = Animator.StringToHash("Working");

    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        switch (state)
        {
            case PrisonerState.Free:     UpdateFree();     break;
            case PrisonerState.Arrested: UpdateArrested(); break;
            case PrisonerState.Working:  UpdateWorking();  break;
        }

        float speed = agent.velocity.magnitude;
        animator?.SetFloat(HashSpeed, speed);
    }

    // ── Free: 돌아다니다 탈출 시도 ──────────────────
    void UpdateFree()
    {
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    // ── Arrested: 감방으로 이동 ──────────────────────
    void UpdateArrested()
    {
        if (cellPoint == null) return;

        agent.SetDestination(cellPoint.position);

        if (agent.remainingDistance < 0.5f)
        {
            state = PrisonerState.InCell;
            gameObject.SetActive(false);  // 감방 안으로 들어감 (UI 카운터 증가)
        }
    }

    // ── Working: 채석장에서 일 ──────────────────────
    void UpdateWorking()
    {
        if (targetRock == null || targetRock.IsDestroyed)
        {
            FindNearestRock();
            return;
        }

        agent.SetDestination(targetRock.transform.position);
        animator?.SetBool(HashWork, agent.remainingDistance < 1.5f);

        if (agent.remainingDistance < 1.5f)
        {
            mineTimer += Time.deltaTime;
            if (mineTimer >= mineInterval)
            {
                mineTimer = 0f;
                targetRock.TakeDamage(mineDamage);
            }
        }
    }

    void FindNearestRock()
    {
        Rock[] rocks = FindObjectsOfType<Rock>();
        float closest = float.MaxValue;
        targetRock = null;

        foreach (var r in rocks)
        {
            if (r.IsDestroyed) continue;
            float dist = Vector3.Distance(transform.position, r.transform.position);
            if (dist < closest) { closest = dist; targetRock = r; }
        }
    }

    // ── Public API ──────────────────────────────────
    public void Arrest(Transform cellPos)
    {
        state     = PrisonerState.Arrested;
        cellPoint = cellPos;
        agent.speed = walkSpeed;
        GameManager.Instance.AddPrisoner();
    }

    public void AssignWork()
    {
        state = PrisonerState.Working;
        agent.speed = workSpeed;
        FindNearestRock();
    }
}
