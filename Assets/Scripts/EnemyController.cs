using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyController : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private int touchDamage = 10;
    [SerializeField] private float attackRange = 1.4f;
    [SerializeField] private float attackCooldownSeconds = 1.0f;
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private bool debugAttacks;

    [Header("Proximity Speed Settings")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 7.5f; // Max player speed is 8.0
    [Space]
    [SerializeField] private float runDistance = 12f;    // Distance to start running
    [SerializeField] private float sprintDistance = 5f; // Distance to start sprinting

    [Header("Animation")]
    [SerializeField] private Animator? animator;
    // Animation hashes for performance (better than strings)
    private static readonly int BlendHash = Animator.StringToHash("Blend");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    [Header("Animation Triggers")]
    private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
    private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
    private NavMeshAgent agent = null!;
    private Transform? target;
    private Health? targetHealth;
    private Health? selfHealth;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<Health>();

        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }

        if (GetComponent<EnemyHitFlash>() == null)
        {
            gameObject.AddComponent<EnemyHitFlash>();
        }

        if (GetComponent<EnemyKnockback>() == null)
        {
            gameObject.AddComponent<EnemyKnockback>();
        }

        if (GetComponent<EnemyStatusEffects>() == null)
        {
            gameObject.AddComponent<EnemyStatusEffects>();
        }
    }

    private void OnEnable()
    {
        if (selfHealth != null)
        {
            selfHealth.Died += OnDied;
            selfHealth.Damaged += OnDamaged;
        }
    }

    private void OnDisable()
    {
        if (selfHealth != null)
        {
            selfHealth.Died -= OnDied;
            selfHealth.Damaged -= OnDamaged;
        }
    }
    
    private void Start()
    {
        PlayerMovement? player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            target = player.transform;
            targetHealth = player.GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (target == null || selfHealth == null || selfHealth.IsDead) return;

        HandleProximitySpeed();
        agent.SetDestination(target.position);

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange)
        {
            TryAttack();
        }
    }

    private void HandleProximitySpeed()
    {
        float distance = Vector3.Distance(transform.position, target.position);

        // Threshold Logic
        if (distance <= sprintDistance)
        {
            agent.speed = sprintSpeed;
        }
        else if (distance <= runDistance)
        {
            agent.speed = runSpeed;
        }
        else
        {
            agent.speed = walkSpeed;
        }

        // Update Animator Blend Tree parameter
        if (animator != null)
        {
            if (HasParameter(animator, BlendHash))
            {
                float normalized = sprintSpeed <= 0f ? 0f : Mathf.Clamp01(agent.velocity.magnitude / sprintSpeed);
                animator.SetFloat(BlendHash, normalized);
            }
        }
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldownSeconds;

        if (animator != null)
        {
            if (HasParameter(animator, AttackTriggerHash))
            {
                animator.SetTrigger(AttackTriggerHash);
            }
        }

        if (targetHealth != null)
        {
            if (debugAttacks) Debug.Log($"[{name}] Attacking player.", this);
            targetHealth.TakeDamage(touchDamage);
        }
    }
    private void OnDamaged(Health health, int damageTaken)
    {
        if (animator != null)
        {
            animator.SetTrigger(HitTriggerHash);
        }
    }
    private void OnDied(Health health)
    {
        // 1. Play Death Animation
        if (animator != null)
        {
            animator.SetTrigger(DeathTriggerHash);
        }

        // 2. Disable AI and Physics so the body stays on the ground
        agent.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        // 3. Update Score
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        // 4. Clean up the object after a delay (e.g., 3 seconds)
        Destroy(gameObject, 3f);
    }

    private static bool HasParameter(Animator a, int hash)
    {
        foreach (AnimatorControllerParameter p in a.parameters)
        {
            if (p.nameHash == hash)
            {
                return true;
            }
        }

        return false;
    }
}
