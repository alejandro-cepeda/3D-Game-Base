using UnityEngine;

public sealed class PlayerRadiationAura : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float damagePerSecond = 20f;
    [SerializeField] private float tickSeconds = 0.25f;
    [SerializeField] private LayerMask hitLayers = ~0;

    [SerializeField] private bool showVisual = true;
    [SerializeField] private Color auraColor = new Color(0.2f, 1f, 0.2f, 0.06f);
    [SerializeField] private float visualHeight = 0.05f;
    [SerializeField] private float edgeThickness = 0.08f;

    private float nextTick;

    private ParticleSystem? particles;
    private static Material? auraMaterial;
    private static Texture2D? circleTexture;

    private void Awake()
    {
        if (showVisual)
        {
            UpdateVisual();
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (Time.time < nextTick)
        {
            return;
        }

        nextTick = Time.time + tickSeconds;

        int damage = Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * tickSeconds));
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, hitLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null)
            {
                continue;
            }

            Health? h = c.GetComponentInParent<Health>();
            if (h == null)
            {
                continue;
            }

            if (h.GetComponent<PlayerMovement>() != null)
            {
                continue;
            }

            h.TakeDamage(damage);
        }
    }

    public void AddRadius(float additionalRadius)
    {
        radius = Mathf.Max(0f, radius + additionalRadius);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (!showVisual)
        {
            return;
        }

        EnsureParticles();
        if (particles == null)
        {
            return;
        }

        particles.transform.localPosition = new Vector3(0f, visualHeight, 0f);

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.radius = radius;
        shape.radiusThickness = Mathf.Clamp01(edgeThickness);

        ParticleSystem.MainModule main = particles.main;
        main.startColor = auraColor;

        ParticleSystemRenderer r = particles.GetComponent<ParticleSystemRenderer>();
        if (r != null && auraMaterial != null)
        {
            r.material = auraMaterial;
        }
    }

    private void EnsureParticles()
    {
        if (particles != null)
        {
            return;
        }

        GameObject obj = new GameObject("RadiationAura", typeof(ParticleSystem));
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0f, visualHeight, 0f);
        obj.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.0f;
        main.startSize = 0.22f;
        main.startColor = auraColor;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 26f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.arc = 360f;
        shape.radiusThickness = Mathf.Clamp01(edgeThickness);
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        if (auraMaterial == null)
        {
            Shader? shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                auraMaterial = new Material(shader);
                auraMaterial.color = auraColor;
                auraMaterial.mainTexture = GetCircleTexture();
            }
        }

        if (auraMaterial != null)
        {
            renderer.material = auraMaterial;
        }

        particles = ps;
    }

    private static Texture2D GetCircleTexture()
    {
        if (circleTexture != null)
        {
            return circleTexture;
        }

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 2) * 0.5f;
        float feather = 3.0f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float a = Mathf.Clamp01((radius - d) / feather);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        circleTexture = tex;
        return circleTexture;
    }
}
