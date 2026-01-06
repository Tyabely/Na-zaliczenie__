using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 1.5f;
    public float updateTargetInterval = 0.5f;
    public float aggroRange = 10f;

    [Header("Damage Settings")]
    public int damageAmount = 1;
    public float damageCooldown = 1f;

    [Header("References")]
    public Transform playerTarget;
    public Rigidbody enemyRigidbody;

    private Vector3 targetPosition;
    private float lastTargetUpdateTime;
    private float lastDamageTime;
    private EnemyHealth enemyHealth;
    private Stats playerStats;
    private bool isDead = false;
    private bool hasAggro = false;
    private Collider enemyCollider;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCollider = GetComponent<Collider>();

        if (enemyRigidbody == null)
            enemyRigidbody = GetComponent<Rigidbody>();

        FindPlayer();

        if (enemyHealth != null)
        {
            // Subskrybuj oba eventy dla bezpieczeństwa
            enemyHealth.OnEnemyDeath += OnEnemyDeathSimple;
            enemyHealth.OnEnemyDeathWithObject += OnEnemyDeathWithObject;
        }
        else
        {
            Debug.LogError($"EnemyHealth not found on {gameObject.name}!");
        }
    }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnEnemyDeath -= OnEnemyDeathSimple;
            enemyHealth.OnEnemyDeathWithObject -= OnEnemyDeathWithObject;
        }
    }

    // Metoda dla starego eventu (bez parametru)
    void OnEnemyDeathSimple()
    {
        HandleDeath();
    }

    // Metoda dla nowego eventu (z GameObject)
    void OnEnemyDeathWithObject(GameObject deadEnemy)
    {
        if (deadEnemy == gameObject)
        {
            HandleDeath();
        }
    }

    void HandleDeath()
    {
        if (isDead) return;

        Debug.Log($"{gameObject.name} - EnemyMovement.HandleDeath() called");

        isDead = true;
        hasAggro = false;

        if (enemyRigidbody != null)
        {
            enemyRigidbody.linearVelocity = Vector3.zero;
            enemyRigidbody.angularVelocity = Vector3.zero;
            enemyRigidbody.isKinematic = true;
            enemyRigidbody.detectCollisions = false;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        this.enabled = false;

        Debug.Log($"{gameObject.name} - EnemyMovement disabled");
    }

    void Update()
    {
        if (isDead) return;

        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        CheckAggro();

        if (hasAggro && Time.time - lastTargetUpdateTime > updateTargetInterval)
        {
            targetPosition = playerTarget.position;
            lastTargetUpdateTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        if (isDead || !hasAggro || enemyRigidbody == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPosition);

        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        if (distanceToPlayer > stoppingDistance)
        {
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            moveDirection.y = 0;

            float currentSpeed = moveSpeed;
            if (distanceToPlayer < stoppingDistance * 2f)
            {
                currentSpeed = Mathf.Lerp(0, moveSpeed, (distanceToPlayer - stoppingDistance) / stoppingDistance);
            }

            Vector3 velocity = moveDirection * currentSpeed;
            velocity.y = enemyRigidbody.linearVelocity.y;
            enemyRigidbody.linearVelocity = velocity;
        }
        else
        {
            enemyRigidbody.linearVelocity = Vector3.zero;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        HandlePlayerCollision(collision.gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        if (isDead) return;
        HandlePlayerCollision(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        HandlePlayerCollision(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        if (isDead) return;
        HandlePlayerCollision(other.gameObject);
    }

    void HandlePlayerCollision(GameObject other)
    {
        if (isDead || (enemyHealth != null && !enemyHealth.IsAlive())) return;

        if (other.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime < damageCooldown)
                return;

            if (playerStats == null)
            {
                playerStats = other.GetComponent<Stats>();
            }

            if (playerStats != null && playerStats.IsAlive())
            {
                playerStats.TakeDamageFromEnemy(damageAmount);
                lastDamageTime = Time.time;

                Rigidbody playerRb = other.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 pushDirection = (other.transform.position - transform.position).normalized;
                    playerRb.AddForce(pushDirection * 3f, ForceMode.Impulse);
                }
            }
        }
    }

    void CheckAggro()
    {
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (!hasAggro && distance <= aggroRange)
        {
            hasAggro = true;
        }
        else if (hasAggro && distance > aggroRange * 1.5f)
        {
            hasAggro = false;
            if (enemyRigidbody != null)
                enemyRigidbody.linearVelocity = Vector3.zero;
        }
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
            playerStats = player.GetComponent<Stats>();
        }
    }

    public bool IsDead()
    {
        return isDead || (enemyHealth != null && !enemyHealth.IsAlive());
    }
}