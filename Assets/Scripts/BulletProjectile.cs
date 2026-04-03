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

    private int remainingPierces;
    private GameObject? owner;
    private Rigidbody rb = null!;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;

        remainingPierces = pierceCount;
        Destroy(gameObject, lifetimeSeconds);

        if (debugHits)
        {
            Debug.Log("[Bullet] Spawned.", this);
        }
    }

    public void Initialize(GameObject projectileOwner, int projectileDamage, float projectileSpeed, int projectilePierceCount)
    {
        owner = projectileOwner;
        damage = projectileDamage;
        speed = projectileSpeed;
        pierceCount = projectilePierceCount;
        remainingPierces = pierceCount;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * (speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
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
