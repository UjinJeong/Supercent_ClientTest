using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public void ScheduleJob()
{
    var data = new NativeArray<float>(count, Allocator.TempJob);
    var job = new MyJob { data = data };
    JobHandle handle = job.Schedule();
    // 데이터는 job이 끝난 뒤에 해제해야 함
    data.Dispose(handle);   // Dispose를 job 완료와 연결
    handle.Complete();      // 필요 시 대기 (Dispose(handle)로 이미 연결하면 안전)
}

struct MyJob : IJob
{
    public NativeArray<float> data;
    public void Execute() { /* ... */ }
}   