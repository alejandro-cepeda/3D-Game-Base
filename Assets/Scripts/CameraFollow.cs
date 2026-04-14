using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);

    public void MultiplyOffset(float multiplier)
    {
        offset *= multiplier;
    }

    public void ZoomOut(float distance)
    {
        float d = Mathf.Max(0f, distance);
        if (d <= 0f)
        {
            return;
        }

        if (Mathf.Abs(offset.x) < 0.01f && Mathf.Abs(offset.z) < 0.01f)
        {
            offset += new Vector3(0f, d, -0.25f * d);
            return;
        }

        Vector3 dir = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.back;
        offset += dir * d;
    }

    public void ZoomOutTopDown(float distance)
    {
        float d = Mathf.Max(0f, distance);
        if (d <= 0f)
        {
            return;
        }

        offset = new Vector3(offset.x, offset.y + d, offset.z);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public Vector3 Offset => offset;

    private void LateUpdate()
    {
        if (target == null)
        {
            PlayerMovement? player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target);
        }
    }
}
