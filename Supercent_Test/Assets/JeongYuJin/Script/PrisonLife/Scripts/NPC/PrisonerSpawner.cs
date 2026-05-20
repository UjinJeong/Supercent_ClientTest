using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrisonerSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject prisonerPrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 8f;
    public int maxFreePrisoners = 5;

    [Header("Working Prisoners")]
    public Transform[] workZonePoints;   // 채석장 내 배치 위치

    private List<Prisoner> freePrisoners = new List<Prisoner>();
    private List<Prisoner> workingPrisoners = new List<Prisoner>();

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 자유 죄수가 너무 많으면 스킵
            CleanupList(freePrisoners);
            if (freePrisoners.Count >= maxFreePrisoners) continue;

            SpawnPrisoner();
        }
    }

    void SpawnPrisoner()
    {
        if (spawnPoints.Length == 0 || prisonerPrefab == null) return;

        Transform spawnPt = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject go = Instantiate(prisonerPrefab, spawnPt.position, spawnPt.rotation);
        Prisoner p = go.GetComponent<Prisoner>();
        if (p != null) freePrisoners.Add(p);
    }

    // 감방에 들어간 죄수를 작업으로 전환
    public void AssignPrisonerToWork()
    {
        if (workZonePoints.Length == 0) return;

        GameObject go = Instantiate(prisonerPrefab,
            workZonePoints[workingPrisoners.Count % workZonePoints.Length].position,
            Quaternion.identity);
        Prisoner p = go.GetComponent<Prisoner>();
        if (p == null) return;

        workingPrisoners.Add(p);
        p.AssignWork();
    }

    void CleanupList(List<Prisoner> list)
    {
        list.RemoveAll(p => p == null);
    }
}
