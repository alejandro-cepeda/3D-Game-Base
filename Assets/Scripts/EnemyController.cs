using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyController : MonoBehaviour
{
    [SerializeField] private int touchDamage = 10;
    [SerializeField] private float attackRange = 1.4f;
    [SerializeField] private float attackCooldownSeconds = 1.0f;
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private bool debugAttacks;

    private NavMeshAgent agent = null!;
    private Transform? target;
    private Health? targetHealth;
    private Health? selfHealth;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<Health>();

        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }
    }

    private void OnEnable()
    {
        if (selfHealth != null)
        {
            selfHealth.Died += OnDied;
        }
    }

    private void OnDisable()
    {
        if (selfHealth != null)
        {
            selfHealth.Died -= OnDied;
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
        if (target == null || selfHealth == null || selfHealth.IsDead)
        {
            return;
        }

        agent.SetDestination(target.position);

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange)
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldownSeconds;

        if (targetHealth != null)
        {
            if (debugAttacks)
            {
                Debug.Log($"[{name}] Attacking player for {touchDamage}.", this);
            }

            targetHealth.TakeDamage(touchDamage);
        }
    }

    private void OnDied(Health health)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        Destroy(gameObject);
    }
}
