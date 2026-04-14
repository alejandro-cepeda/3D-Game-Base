using UnityEngine;
using System;
using System.Collections;

public sealed class PlayerCombat : MonoBehaviour
{
    public enum WeaponType
    {
        Melee,
        Gun
    }

    public enum WeaponId
    {
        Pistol,
        Shotgun,
        AssaultRifle
    }

    public enum BulletType
    {
        Normal,
        Freeze,
        Poison,
        Explosive
    }

    [System.Serializable]
    private struct WeaponStats
    {
        public int damage;
        public float fireRateSeconds;
        public float bulletSpeed;
        public float bulletLifetimeSeconds;
        public int pierceCount;

        public int pellets;
        public float spreadDegrees;
    }

    [SerializeField] private WeaponType startingWeapon = WeaponType.Gun;
    [SerializeField] private WeaponId startingGun = WeaponId.Pistol;
    [SerializeField] private int damage = 25;
    [SerializeField] private float meleeRange = 3f;
    [SerializeField] private float gunRange = 120f;
    [SerializeField] private BulletProjectile? bulletPrefab;
    [SerializeField] private Transform? muzzle;
    [SerializeField] private float bulletSpeed = 45f;
    [SerializeField] private int bulletPierceCount;
    [SerializeField] private float bulletLifetimeSeconds = 2.5f;
    [SerializeField] private bool gunAimUsesMouse = true;
    [SerializeField] private float gunAimMaxDistance = 200f;
    [SerializeField] private bool gunAimOnMuzzleHeightPlane = true;
    [SerializeField] private float fireRateSeconds = 0.2f;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private bool debugHits;
    [SerializeField] private float originHeight = 1.4f;
    [SerializeField] private bool aimAtMousePosition = true;

    [Header("Muzzle Flash")]
    [SerializeField] private bool enableMuzzleFlash = true;
    [SerializeField] private Color muzzleFlashColor = new Color(1f, 0.85f, 0.4f, 1f);
    [SerializeField] private float muzzleFlashLightIntensity = 10f;
    [SerializeField] private float muzzleFlashLightRange = 6f;
    [SerializeField] private float muzzleFlashDurationSeconds = 0.05f;
    [SerializeField] private float muzzleFlashParticleSize = 0.08f;

    private float nextFireTime;
    private WeaponId currentGun;
    private float spreadMultiplier = 1f;
    private int pierceBonus;
    private BulletType currentBulletType = BulletType.Normal;

    public event Action<WeaponId>? GunChanged;

    private Light? muzzleLight;
    private ParticleSystem? muzzleParticles;
    private Coroutine? muzzleFlashRoutine;

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

    private void Awake()
    {
        currentGun = startingGun;
        ApplyGunStats(currentGun);
        GunChanged?.Invoke(currentGun);

        if (enableMuzzleFlash)
        {
            EnsureMuzzleFlashObjects();
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

                Vector3 aimDirection = aimRay.direction;

                if (gunAimOnMuzzleHeightPlane)
                {
                    Plane plane = new Plane(Vector3.up, muzzle.position);
                    if (plane.Raycast(aimRay, out float enter))
                    {
                        Vector3 pointOnPlane = aimRay.GetPoint(enter);
                        Vector3 dir = pointOnPlane - muzzle.position;
                        if (dir.sqrMagnitude > 0.0001f)
                        {
                            aimDirection = dir.normalized;
                        }
                    }
                }

                rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            }

            WeaponStats stats = GetCurrentStats();
            int pellets = Mathf.Max(1, stats.pellets);
            float spread = stats.spreadDegrees * spreadMultiplier;

            for (int i = 0; i < pellets; i++)
            {
                Quaternion pelletRotation = rotation;
                if (spread > 0f)
                {
                    float yaw = UnityEngine.Random.Range(-spread, spread);
                    pelletRotation = rotation * Quaternion.Euler(0f, yaw, 0f);
                }

                Vector3 spawnPos = muzzle.position + (pelletRotation * Vector3.forward * 0.35f);
                BulletProjectile projectile = Instantiate(bulletPrefab, spawnPos, pelletRotation);
                projectile.Initialize(
                    projectileOwner: gameObject,
                    projectileDamage: damage,
                    projectileSpeed: bulletSpeed,
                    projectilePierceCount: currentBulletType == BulletType.Explosive ? 0 : bulletPierceCount,
                    bulletType: currentBulletType
                );
                projectile.SetLifetime(bulletLifetimeSeconds);
                projectile.MultiplyHitRadius(1.5f);

                if (currentGun == WeaponId.Shotgun)
                {
                    projectile.ConfigureVisuals(2.5f, true, new Color(1f, 0.8f, 0.2f, 1f));
                }
                else if (currentGun == WeaponId.AssaultRifle)
                {
                    projectile.ConfigureVisuals(1.5f, true, new Color(0.6f, 0.9f, 1f, 1f));
                }
            }

            if (debugHits)
            {
                Debug.Log($"[PlayerCombat] Fired {pellets} projectile(s) with {currentGun}. Damage: {damage} Speed: {bulletSpeed} Lifetime: {bulletLifetimeSeconds} Pierce: {bulletPierceCount}", this);
            }

            TriggerMuzzleFlash();

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

    public void AddBulletLifetime(float additionalSeconds)
    {
        bulletLifetimeSeconds = Mathf.Max(0.1f, bulletLifetimeSeconds + additionalSeconds);
    }

    public void AddPierce(int additionalPierce)
    {
        pierceBonus = Mathf.Max(0, pierceBonus + additionalPierce);
        ApplyGunStats(currentGun);
    }

    public void ImproveAccuracy(float multiplier)
    {
        spreadMultiplier = Mathf.Clamp(spreadMultiplier * Mathf.Clamp01(multiplier), 0f, 1f);
    }

    public void SetPerfectAccuracy()
    {
        spreadMultiplier = 0f;
    }

    public WeaponId CurrentGun => currentGun;

    public Transform? Muzzle => muzzle;

    public BulletType CurrentBulletType => currentBulletType;

    public void SetBulletType(BulletType type)
    {
        currentBulletType = type;
    }

    public void SetGun(WeaponId weapon)
    {
        currentGun = weapon;
        ApplyGunStats(currentGun);
        nextFireTime = 0f;

        GunChanged?.Invoke(currentGun);

        if (debugHits)
        {
            Debug.Log($"[PlayerCombat] Switched gun to {currentGun}. Damage: {damage} FireRateSeconds: {fireRateSeconds} BulletSpeed: {bulletSpeed} Lifetime: {bulletLifetimeSeconds}", this);
        }
    }

    private void TriggerMuzzleFlash()
    {
        if (!enableMuzzleFlash)
        {
            return;
        }

        if (muzzle == null)
        {
            return;
        }

        EnsureMuzzleFlashObjects();

        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
            muzzleLight.color = muzzleFlashColor;
            muzzleLight.intensity = muzzleFlashLightIntensity;
            muzzleLight.range = muzzleFlashLightRange;
        }

        if (muzzleParticles != null)
        {
            muzzleParticles.Play(true);
        }

        if (muzzleFlashRoutine != null)
        {
            StopCoroutine(muzzleFlashRoutine);
        }

        muzzleFlashRoutine = StartCoroutine(MuzzleFlashOffRoutine());
    }

    private IEnumerator MuzzleFlashOffRoutine()
    {
        yield return new WaitForSecondsRealtime(muzzleFlashDurationSeconds);
        if (muzzleLight != null)
        {
            muzzleLight.enabled = false;
        }

        muzzleFlashRoutine = null;
    }

    private void EnsureMuzzleFlashObjects()
    {
        if (muzzle == null)
        {
            return;
        }

        if (muzzleLight == null)
        {
            GameObject lightObject = new GameObject("MuzzleFlashLight", typeof(Light));
            lightObject.transform.SetParent(muzzle, false);
            lightObject.transform.localPosition = Vector3.zero;
            lightObject.transform.localRotation = Quaternion.identity;

            Light l = lightObject.GetComponent<Light>();
            l.type = LightType.Point;
            l.enabled = false;
            l.intensity = muzzleFlashLightIntensity;
            l.range = muzzleFlashLightRange;
            l.color = muzzleFlashColor;
            muzzleLight = l;
        }

        if (muzzleParticles == null)
        {
            GameObject particleObject = new GameObject("MuzzleFlashParticles", typeof(ParticleSystem));
            particleObject.transform.SetParent(muzzle, false);
            particleObject.transform.localPosition = Vector3.zero;
            particleObject.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = particleObject.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.startLifetime = 0.05f;
            main.startSpeed = 0.0f;
            main.startSize = muzzleFlashParticleSize;
            main.startColor = muzzleFlashColor;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.02f;
            shape.length = 0.05f;

            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;

            muzzleParticles = ps;
        }
    }

    private WeaponStats GetCurrentStats()
    {
        return currentGun switch
        {
            WeaponId.Pistol => new WeaponStats { pellets = 1, spreadDegrees = 0f },
            WeaponId.Shotgun => new WeaponStats { pellets = 6, spreadDegrees = 14f },
            _ => new WeaponStats { pellets = 1, spreadDegrees = 2.5f }
        };
    }

    private void ApplyGunStats(WeaponId weapon)
    {
        WeaponStats stats = weapon switch
        {
            WeaponId.Pistol => new WeaponStats
            {
                damage = 25,
                fireRateSeconds = 0.5f,
                bulletSpeed = 55f,
                bulletLifetimeSeconds = 1.6f,
                pierceCount = 0,
                pellets = 1,
                spreadDegrees = 0f
            },
            WeaponId.Shotgun => new WeaponStats
            {
                damage = 20,
                fireRateSeconds = 1.0f,
                bulletSpeed = 45f,
                bulletLifetimeSeconds = 0.45f,
                pierceCount = 0,
                pellets = 6,
                spreadDegrees = 14f
            },
            _ => new WeaponStats
            {
                damage = 25,
                fireRateSeconds = 0.25f,
                bulletSpeed = 70f,
                bulletLifetimeSeconds = 1.9f,
                pierceCount = 0,
                pellets = 1,
                spreadDegrees = 2.5f
            }
        };

        damage = stats.damage;
        fireRateSeconds = stats.fireRateSeconds;
        bulletSpeed = stats.bulletSpeed;
        bulletLifetimeSeconds = stats.bulletLifetimeSeconds;
        bulletPierceCount = stats.pierceCount + pierceBonus;
    }
}
