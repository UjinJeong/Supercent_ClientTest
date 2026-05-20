using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Prisoner : MonoBehaviour
{
    // 죄수 상태 열거형: 자유, 체포, 감방, 작업 중
    public enum PrisonerState { Free, Arrested, InCell, Working }

    [Header("Stats")]
    // 현재 상태
    public PrisonerState state = PrisonerState.Free;
    // 걷기 속도
    public float walkSpeed = 2f;
    // 작업(채굴) 시 이동 속도
    public float workSpeed = 1.5f;

    [Header("Work")]
    // 채굴 간격(초)
    public float mineInterval = 1.5f;
    // 한 번 채굴 시 가하는 데미지
    public float mineDamage = 5f;

    // 내부 컴포넌트 캐시
    private NavMeshAgent agent;
    private float mineTimer;

    // 작업 대상 바위 및 감방 위치 참조
    private Rock targetRock;
    private Transform cellPoint;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogWarning($"Prisoner: NavMeshAgent가 없어 자동으로 추가합니다. ({name})");
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
    }

    void Update()
    {
        if (agent == null) return;

        // 상태에 따라 각 동작 업데이트
        switch (state)
        {
            case PrisonerState.Free:     UpdateFree();     break;
            case PrisonerState.Arrested: UpdateArrested(); break;
            case PrisonerState.Working:  UpdateWorking();  break;
        }
    }

    // ── Free: 돌아다니는 동작
    void UpdateFree()
    {
        if (agent == null) return;

        // 목적지가 없거나 거의 도달한 경우 새로운 목적지 설정
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    // ── Arrested: 감방으로 이동하여 비활성화 처리
    void UpdateArrested()
    {
        if (agent == null || cellPoint == null) return;

        // 감방 위치로 이동
        agent.SetDestination(cellPoint.position);

        // 감방에 도착하면 상태 변경 및 비활성화(감금 처리)
        if (agent.remainingDistance < 0.5f)
        {
            state = PrisonerState.InCell;
            gameObject.SetActive(false);  // 감방 안으로 들어간 것으로 처리
        }
    }

    // ── Working: 채석장에서 일하는 동작
    void UpdateWorking()
    {
        if (agent == null) return;

        // 현재 목표 바위가 없거나 이미 파괴된 경우 새로운 바위 탐색
        if (targetRock == null || targetRock.IsDestroyed)
        {
            FindNearestRock();
            return;
        }

        // 목표 바위 위치로 이동 설정
        agent.SetDestination(targetRock.transform.position);

        // 바위에 도착하면 채굴 타이머로 간헐적 데미지 적용
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

    // 가장 가까운 파괴되지 않은 바위 찾기
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
    // 체포 처리: 감방 위치를 전달받아 이동 시작
    public void Arrest(Transform cellPos)
    {
        state     = PrisonerState.Arrested;
        cellPoint = cellPos;
        if (agent != null) agent.speed = walkSpeed;
        GameManager.Instance.AddPrisoner();
    }

    // 작업자로 전환: 작업 상태로 변경하고 바위 탐색 시작
    public void AssignWork()
    {
        state = PrisonerState.Working;
        if (agent != null) agent.speed = workSpeed;
        FindNearestRock();
    }
}
