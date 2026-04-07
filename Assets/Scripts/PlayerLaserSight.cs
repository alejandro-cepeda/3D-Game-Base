using UnityEngine;

[RequireComponent(typeof(PlayerCombat))]
public sealed class PlayerLaserSight : MonoBehaviour
{
    [SerializeField] private bool enabledOnStart;
    [SerializeField] private float maxDistance = 200f;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private Color hitEnemyColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color noHitColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private LayerMask hitLayers = ~0;

    private PlayerCombat combat = null!;
    private LineRenderer line = null!;
    private static Material? lineMaterial;

    public bool IsEnabled { get; private set; }

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();

        line = gameObject.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
        }

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        if (lineMaterial == null)
        {
            Shader? shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                lineMaterial = new Material(shader);
            }
        }

        if (lineMaterial != null)
        {
            line.material = lineMaterial;
        }

        SetEnabled(enabledOnStart);
    }

    private void LateUpdate()
    {
        if (!IsEnabled || Time.timeScale == 0f)
        {
            if (line.enabled)
            {
                line.enabled = false;
            }

            return;
        }

        Transform? muzzle = combat.Muzzle;
        if (muzzle == null)
        {
            line.enabled = false;
            return;
        }

        Vector3 origin = muzzle.position;
        Vector3 direction = muzzle.forward;

        Ray ray = new Ray(origin, direction);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, hitLayers, QueryTriggerInteraction.Ignore);
        if (hits.Length > 1)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        }

        Vector3 end = origin + direction * maxDistance;
        bool hitEnemy = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.GetComponentInParent<PlayerMovement>() != null)
            {
                continue;
            }

            end = hit.point;

            Health? h = hit.collider.GetComponentInParent<Health>();
            if (h != null && h.GetComponent<PlayerMovement>() == null)
            {
                hitEnemy = true;
            }

            break;
        }

        line.enabled = true;
        line.startColor = hitEnemy ? hitEnemyColor : noHitColor;
        line.endColor = hitEnemy ? hitEnemyColor : noHitColor;
        line.SetPosition(0, origin);
        line.SetPosition(1, end);
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        line.enabled = enabled;
    }
}
