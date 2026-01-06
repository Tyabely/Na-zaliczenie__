using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private EnemyHealth enemyHealth;
    private EnemyMovement enemyMovement;

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyMovement = GetComponent<EnemyMovement>();

        if (enemyHealth == null)
        {
            enemyHealth = gameObject.AddComponent<EnemyHealth>();
        }
    }

    void Start()
    {
        if (enemyHealth != null)
        {
            // Subskrybuj oba eventy
            enemyHealth.OnEnemyDeath += HandleEnemyDeathSimple;
            enemyHealth.OnEnemyDeathWithObject += HandleEnemyDeathWithObject;
        }
    }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnEnemyDeath -= HandleEnemyDeathSimple;
            enemyHealth.OnEnemyDeathWithObject -= HandleEnemyDeathWithObject;
        }
    }

    void HandleEnemyDeathSimple()
    {
        HandleDeath();
    }

    void HandleEnemyDeathWithObject(GameObject deadEnemy)
    {
        if (deadEnemy == gameObject)
        {
            HandleDeath();
        }
    }

    void HandleDeath()
    {
        Debug.Log($"{name} - EnemyBehaviour: Death handled");

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this && script.enabled)
            {
                script.enabled = false;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
    }

    public bool IsAlive()
    {
        return enemyHealth != null && enemyHealth.IsAlive();
    }
}