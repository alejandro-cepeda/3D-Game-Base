using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyStatusEffects : MonoBehaviour
{
    [SerializeField] private Color frozenTint = new Color(0.2f, 0.5f, 1f, 1f);

    private NavMeshAgent? agent;
    private Animator? animator;
    private Renderer[] renderers = System.Array.Empty<Renderer>();

    private Coroutine? freezeRoutine;
    private Coroutine? poisonRoutine;

    private Material? frozenMaterial;
    private Material[][] originalMaterials = System.Array.Empty<Material[]>();

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        renderers = GetComponentsInChildren<Renderer>(true);

        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterials : System.Array.Empty<Material>();
        }

        Shader? shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null)
        {
            frozenMaterial = new Material(shader);
            frozenMaterial.color = frozenTint;
        }
    }

    public void ApplyFreeze(float seconds)
    {
        if (seconds <= 0f)
        {
            return;
        }

        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
        }

        freezeRoutine = StartCoroutine(FreezeRoutine(seconds));
    }

    public void ApplyPoison(float damagePerSecond, float seconds)
    {
        if (damagePerSecond <= 0f || seconds <= 0f)
        {
            return;
        }

        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
        }

        poisonRoutine = StartCoroutine(PoisonRoutine(damagePerSecond, seconds));
    }

    private IEnumerator FreezeRoutine(float seconds)
    {
        bool canStopAgent = agent != null && agent.enabled && agent.isOnNavMesh;
        bool wasStopped = canStopAgent && agent != null && agent.isStopped;
        if (canStopAgent && agent != null)
        {
            agent.isStopped = true;
        }

        float prevAnimSpeed = 1f;
        if (animator != null)
        {
            prevAnimSpeed = animator.speed;
            animator.speed = 0f;
        }

        ApplyFrozenVisual(true);
        yield return new WaitForSecondsRealtime(seconds);
        ApplyFrozenVisual(false);

        if (animator != null)
        {
            animator.speed = prevAnimSpeed;
        }

        if (canStopAgent && agent != null)
        {
            agent.isStopped = wasStopped;
        }

        freezeRoutine = null;
    }

    private IEnumerator PoisonRoutine(float damagePerSecond, float seconds)
    {
        float remaining = seconds;
        float tick = 0.25f;
        float accumulator = 0f;

        while (remaining > 0f)
        {
            float dt = Mathf.Min(tick, remaining);
            remaining -= dt;

            accumulator += damagePerSecond * dt;
            int damage = Mathf.FloorToInt(accumulator);
            if (damage > 0)
            {
                accumulator -= damage;
                Health? h = GetComponent<Health>();
                if (h != null)
                {
                    h.TakeDamage(damage);
                }
            }

            yield return new WaitForSecondsRealtime(dt);
        }

        poisonRoutine = null;
    }

    private void ApplyFrozenVisual(bool enabled)
    {
        if (frozenMaterial == null)
        {
            return;
        }

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
                    mats[m] = frozenMaterial;
                }

                r.sharedMaterials = mats;
            }
            else
            {
                r.sharedMaterials = originalMaterials[i];
            }
        }
    }
}
