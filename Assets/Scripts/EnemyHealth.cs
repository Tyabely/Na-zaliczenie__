using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 50;
    public int currentHealth;

    // Dwie wersje event�w dla kompatybilno�ci
    public event Action OnEnemyDeath; // Stara wersja (bez parametru)
    public event Action<GameObject> OnEnemyDeathWithObject; // Nowa wersja (z GameObject)

    [Header("Effects")]
    public GameObject deathEffect;
    public AudioClip deathSound;
    public AudioClip hitSound;

    private AudioSource audioSource;
    private bool isDead = false;
    private Collider enemyCollider;
    private Rigidbody enemyRigidbody;
    private Renderer enemyRenderer;

    void Awake()
    {
        currentHealth = maxHealth;

        enemyCollider = GetComponent<Collider>();
        enemyRigidbody = GetComponent<Rigidbody>();
        enemyRenderer = GetComponent<Renderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound, 0.5f);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"{gameObject.name} - EnemyHealth.Die() called");

        // 1. Wy��cz fizyk�
        DisablePhysicsAndCollision();

        // 2. Wywo�aj OBA eventy (dla kompatybilno�ci)
        OnEnemyDeath?.Invoke(); // Stara wersja
        OnEnemyDeathWithObject?.Invoke(gameObject); // Nowa wersja

        // 3. Efekty
        PlayDeathEffects();

        // 4. Zniszcz
        Destroy(gameObject, 0.5f);
    }

    void DisablePhysicsAndCollision()
    {
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (enemyRigidbody != null)
        {
            enemyRigidbody.linearVelocity = Vector3.zero;
            enemyRigidbody.angularVelocity = Vector3.zero;
            enemyRigidbody.isKinematic = true;
            enemyRigidbody.detectCollisions = false;
        }

        if (enemyRenderer != null)
        {
            enemyRenderer.enabled = false;
        }

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }
    }

    void PlayDeathEffects()
    {
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 0.7f);
        }

        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }

    public bool IsAlive()
    {
        return !isDead;
    }
}