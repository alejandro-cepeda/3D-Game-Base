using UnityEngine;

public sealed class PlayerCombat : MonoBehaviour
{
    public enum WeaponType
    {
        Melee,
        Gun
    }

    [SerializeField] private WeaponType startingWeapon = WeaponType.Gun;
    [SerializeField] private int damage = 25;
    [SerializeField] private float meleeRange = 3f;
    [SerializeField] private float gunRange = 120f;
    [SerializeField] private BulletProjectile? bulletPrefab;
    [SerializeField] private Transform? muzzle;
    [SerializeField] private float bulletSpeed = 45f;
    [SerializeField] private int bulletPierceCount;
    [SerializeField] private bool gunAimUsesMouse = true;
    [SerializeField] private float gunAimMaxDistance = 200f;
    [SerializeField] private float fireRateSeconds = 0.2f;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private bool debugHits;
    [SerializeField] private float originHeight = 1.4f;
    [SerializeField] private bool aimAtMousePosition = true;

    private float nextFireTime;

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireRateSeconds;

        if (startingWeapon == WeaponType.Gun && bulletPrefab != null && muzzle != null)
        {
            Quaternion rotation = muzzle.rotation;

            Camera? aimCamera = Camera.main;
            if (gunAimUsesMouse && aimCamera != null)
            {
                Ray aimRay = aimCamera.ScreenPointToRay(Input.mousePosition);
                Vector3 aimPoint = aimRay.origin + aimRay.direction * gunAimMaxDistance;
                if (Physics.Raycast(aimRay, out RaycastHit aimHit, gunAimMaxDistance, hitLayers, QueryTriggerInteraction.Ignore))
                {
                    aimPoint = aimHit.point;
                }

                Vector3 dir = aimPoint - muzzle.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }

            BulletProjectile projectile = Instantiate(bulletPrefab, muzzle.position, rotation);
            projectile.Initialize(gameObject, damage, bulletSpeed, bulletPierceCount);

            if (debugHits)
            {
                Debug.Log($"[PlayerCombat] Fired bullet. Damage: {damage} Speed: {bulletSpeed} Pierce: {bulletPierceCount}", this);
            }

            return;
        }

        Camera? camera = Camera.main;

        float actualRange = startingWeapon == WeaponType.Melee ? meleeRange : gunRange;

        Ray ray;
        if (camera != null)
        {
            ray = aimAtMousePosition ? camera.ScreenPointToRay(Input.mousePosition) : camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else
        {
            Vector3 origin = transform.position + Vector3.up * originHeight;
            ray = new Ray(origin, transform.forward);
        }

        if (debugHits)
        {
            Debug.DrawRay(ray.origin, ray.direction * actualRange, Color.red, 0.1f);
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, actualRange, hitLayers, QueryTriggerInteraction.Collide);
        if (hits.Length == 0)
        {
            if (debugHits)
            {
                string cameraInfo = Camera.main != null ? $"{Camera.main.name} (tagged MainCamera)" : "<none>";
                Debug.Log($"[PlayerCombat] Attack missed. Camera: {cameraInfo}. Origin: {ray.origin} Dir: {ray.direction}", this);
            }

            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit? validHit = null;
        foreach (RaycastHit h in hits)
        {
            if (h.collider == null)
            {
                continue;
            }

            if (h.collider.GetComponentInParent<PlayerMovement>() != null)
            {
                continue;
            }

            validHit = h;
            break;
        }

        if (validHit == null)
        {
            if (debugHits)
            {
                Debug.Log($"[PlayerCombat] Hits found ({hits.Length}) but none valid (non-player). First hit: {hits[0].collider.name}", this);
            }

            return;
        }

        RaycastHit hit = validHit.Value;

        Health? health = hit.collider.GetComponentInParent<Health>();
        if (health == null)
        {
            if (debugHits)
            {
                Debug.Log($"[PlayerCombat] Hit {hit.collider.name} but no Health found.", this);
            }

            return;
        }

        if (health.gameObject.GetComponent<PlayerMovement>() != null)
        {
            if (debugHits)
            {
                Debug.Log("[PlayerCombat] Hit player; ignoring.", this);
            }

            return;
        }

        if (debugHits)
        {
            Debug.Log($"[PlayerCombat] Hit {health.name} for {damage}.", this);
        }

        health.TakeDamage(damage);
    }
}
