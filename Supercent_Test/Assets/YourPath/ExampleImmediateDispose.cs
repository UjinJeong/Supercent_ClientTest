using Unity.Collections;

void DoWork()
{
    // 한 프레임 내에 사용하고 바로 없애는 경우
    using (var arr = new NativeArray<int>(100, Allocator.Temp))
    {
        // 사용
    } // 여기서 즉시 Dispose
}