using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrisonerSpawner : MonoBehaviour
{
    // 죄수 생성 및 관리 담당 스포너

        [Header("Spawning")]
    // 생성할 죄수 프리팹
    public GameObject prisonerPrefab;
    // 스폰 지점 배열
    public Transform[] spawnPoints;
    // 죄수 자동 스폰 간격(초)
    public float spawnInterval = 8f;
    // 자유 상태로 유지할 수 있는 최대 죄수 수
    public int maxFreePrisoners = 5;

    [Header("Working Prisoners")]
    // 작업(채석장 등)에 배치할 위치들
    public Transform[] workZonePoints;   // 채석장 내 배치 위치

    // 현재 자유 상태(대기 중)인 죄수 리스트
    private List<Prisoner> freePrisoners = new List<Prisoner>();
    // 작업 중인 죄수 리스트
    private List<Prisoner> workingPrisoners = new List<Prisoner>();

    void Start()
    {
        // 스폰 루틴 시작
        StartCoroutine(SpawnRoutine());
    }

    // 주기적으로 새로운 죄수를 생성하는 코루틴
    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 자유 죄수 리스트 정리 후 최대치를 넘으면 스폰하지 않음
            CleanupList(freePrisoners);
            if (freePrisoners.Count >= maxFreePrisoners) continue;

            SpawnPrisoner();
        }
    }

    // 실제로 죄수를 스폰하여 자유 죄수 리스트에 추가
    void SpawnPrisoner()
    {
        if (spawnPoints.Length == 0 || prisonerPrefab == null) return;

        Transform spawnPt = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject go = Instantiate(prisonerPrefab, spawnPt.position, spawnPt.rotation);
        Prisoner p = go.GetComponent<Prisoner>();
        if (p != null) freePrisoners.Add(p);
    }

    // 감방에 있던 죄수를 작업자로 전환하여 작업 존에 배치
    public void AssignPrisonerToWork()
    {
        if (workZonePoints.Length == 0) return;

        // 작업자 수에 따라 순환 배치 (원형 인덱싱)
        GameObject go = Instantiate(prisonerPrefab,
            workZonePoints[workingPrisoners.Count % workZonePoints.Length].position,
            Quaternion.identity);
        Prisoner p = go.GetComponent<Prisoner>();
        if (p == null) return;

        workingPrisoners.Add(p);
        p.AssignWork();
    }

    // 리스트에서 null 참조(파괴된 객체) 제거
    void CleanupList(List<Prisoner> list)
    {
        list.RemoveAll(p => p == null);
    }
}
