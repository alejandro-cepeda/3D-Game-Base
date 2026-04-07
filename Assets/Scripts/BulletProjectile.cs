using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class BulletProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private float speed = 35f;
    [SerializeField] private float lifetimeSeconds = 3f;
    [SerializeField] private int pierceCount;
    [SerializeField] private bool debugHits;
    [SerializeField] private bool useTrail;
    [SerializeField] private float trailTime = 0.08f;
    [SerializeField] private float trailStartWidth = 0.05f;
    [SerializeField] private float trailEndWidth = 0.0f;
    [SerializeField] private float armDelaySeconds = 0.03f;

    private int remainingPierces;
    private GameObject? owner;
    private Rigidbody rb = null!;
    private float remainingLifetime;
    private float remainingArmDelay;
    private bool armed;

    private TrailRenderer? trail;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;

        remainingArmDelay = armDelaySeconds;
        armed = remainingArmDelay <= 0f;

        remainingPierces = pierceCount;
        remainingLifetime = lifetimeSeconds;

        if (useTrail)
        {
            EnsureTrail(Color.white);
        }

        if (debugHits)
        {
            Debug.Log("[Bullet] Spawned.", this);
        }
    }

    public void ConfigureVisuals(float scaleMultiplier, bool enableTrail, Color trailColor)
    {
        if (scaleMultiplier != 1f)
        {
            transform.localScale = transform.localScale * scaleMultiplier;
        }

        useTrail = enableTrail;
        if (useTrail)
        {
            EnsureTrail(trailColor);
        }
    }

    private void EnsureTrail(Color color)
    {
        if (trail == null)
        {
            trail = gameObject.GetComponent<TrailRenderer>();
        }

        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }

        trail.time = trailTime;
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        Shader? shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = color;
            trail.material = mat;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trail.colorGradient = gradient;
    }

    public void Initialize(GameObject projectileOwner, int projectileDamage, float projectileSpeed, int projectilePierceCount)
    {
        owner = projectileOwner;
        damage = projectileDamage;
        speed = projectileSpeed;
        pierceCount = projectilePierceCount;
        remainingPierces = pierceCount;
    }

    public void SetLifetime(float seconds)
    {
        remainingLifetime = Mathf.Max(0.05f, seconds);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * (speed * Time.fixedDeltaTime));

        if (!armed)
        {
            remainingArmDelay -= Time.fixedDeltaTime;
            if (remainingArmDelay <= 0f)
            {
                armed = true;
            }
        }

        remainingLifetime -= Time.fixedDeltaTime;
        if (remainingLifetime <= 0f)
        {
            if (debugHits)
            {
                Debug.Log("[Bullet] Expired.", this);
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!armed)
        {
            return;
        }

        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        if (debugHits)
        {
            Debug.Log($"[Bullet] Hit collider: {other.name} (root: {other.transform.root.name})", this);
        }

        Health? health = other.GetComponentInParent<Health>();
        if (health != null)
        {
            if (debugHits)
            {
                Debug.Log($"[Bullet] Applying {damage} damage to {health.name}.", this);
            }

            health.TakeDamage(damage);
        }
        else
        {
            if (debugHits)
            {
                Debug.Log("[Bullet] No Health found on hit object.", this);
            }
        }

        if (remainingPierces > 0)
        {
            remainingPierces--;
            return;
        }

        if (debugHits)
        {
            Debug.Log("[Bullet] Destroying bullet (no pierce remaining).", this);
        }

        Destroy(gameObject);
    }
}
