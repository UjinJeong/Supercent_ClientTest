using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Isometric Settings")]
    public Vector3 offset = new Vector3(0f, 12f, -8f);
    public float smoothSpeed = 8f;

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 xBounds = new Vector2(-20f, 20f);
    public Vector2 zBounds = new Vector2(-20f, 20f);

    private Vector3 desiredPos;

    void LateUpdate()
    {
        if (target == null) return;

        desiredPos = target.position + offset;

        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, xBounds.x, xBounds.y);
            desiredPos.z = Mathf.Clamp(desiredPos.z, zBounds.x, zBounds.y);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
