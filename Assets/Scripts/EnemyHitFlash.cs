using System.Collections;
using UnityEngine;

public sealed class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashDurationSeconds = 0.1f;
    [SerializeField] private bool debug;

    [SerializeField] private bool useMaterialSwap;
    [SerializeField] private Material? flashMaterial;

    private Health? health;
    private Renderer[] renderers = System.Array.Empty<Renderer>();
    private Coroutine? running;

    private Material[][] originalMaterials = System.Array.Empty<Material[]>();

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Material? runtimeFlashMaterial;

    private void Awake()
    {
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<Renderer>(true);

        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterials : System.Array.Empty<Material>();
        }

        if (debug && renderers.Length == 0)
        {
            Debug.Log("[EnemyHitFlash] No renderers found.", this);
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
        }
    }

    private void OnDamaged(Health h, int amount)
    {
        if (debug)
        {
            Debug.Log($"[EnemyHitFlash] Damaged for {amount}.", this);
        }

        if (running != null)
        {
            StopCoroutine(running);
        }

        float intensity = Mathf.Clamp01(amount / 50f);
        Color applied = Color.Lerp(flashColor * 0.6f, flashColor, intensity);

        if (useMaterialSwap)
        {
            Material? materialToUse = flashMaterial != null ? flashMaterial : GetOrCreateRuntimeFlashMaterial();
            if (materialToUse != null)
            {
                materialToUse.SetColor(BaseColorId, applied);
                materialToUse.SetColor(ColorId, applied);
                materialToUse.SetColor(EmissionColorId, applied);
            }
        }

        running = StartCoroutine(FlashRoutine(applied));
    }

    private IEnumerator FlashRoutine(Color applied)
    {
        ApplyFlash(true, applied);
        yield return new WaitForSecondsRealtime(flashDurationSeconds);
        ApplyFlash(false, applied);
        running = null;
    }

    private void ApplyFlash(bool enabled, Color applied)
    {
        if (useMaterialSwap)
        {
            Material? materialToUse = flashMaterial != null ? flashMaterial : GetOrCreateRuntimeFlashMaterial();
            if (materialToUse == null)
            {
                if (debug)
                {
                    Debug.Log("[EnemyHitFlash] Material swap enabled but no usable Flash Material.", this);
                }

                return;
            }

            materialToUse.SetColor(BaseColorId, applied);
            materialToUse.SetColor(ColorId, applied);
            materialToUse.SetColor(EmissionColorId, applied);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                {
                    continue;
                }

                if (enabled)
                {
                    Material[] mats = r.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        mats[m] = materialToUse;
                    }

                    r.sharedMaterials = mats;
                }
                else
                {
                    r.sharedMaterials = originalMaterials[i];
                }
            }

            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
            {
                continue;
            }

            int materialCount = r.sharedMaterials != null ? r.sharedMaterials.Length : 1;
            for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
            {
                if (enabled)
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block, materialIndex);
                    block.SetColor(BaseColorId, applied);
                    block.SetColor(ColorId, applied);
                    block.SetColor(EmissionColorId, applied);
                    r.SetPropertyBlock(block, materialIndex);
                }
                else
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    r.SetPropertyBlock(block, materialIndex);
                }
            }
        }
    }

    private Material? GetOrCreateRuntimeFlashMaterial()
    {
        if (runtimeFlashMaterial != null)
        {
            return runtimeFlashMaterial;
        }

        Shader? shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            return null;
        }

        runtimeFlashMaterial = new Material(shader);
        runtimeFlashMaterial.SetColor(BaseColorId, flashColor);
        runtimeFlashMaterial.SetColor(ColorId, flashColor);
        runtimeFlashMaterial.SetColor(EmissionColorId, flashColor);
        return runtimeFlashMaterial;
    }
}
