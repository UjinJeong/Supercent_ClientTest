using Unity.Collections;
using UnityEngine;

public class ReuseExample : MonoBehaviour
{
    NativeArray<int> buffer;

    void Start()
    {
        buffer = new NativeArray<int>(1024, Allocator.Persistent);
    }

    void OnDestroy()
    {
        if (buffer.IsCreated) buffer.Dispose();
    }
}