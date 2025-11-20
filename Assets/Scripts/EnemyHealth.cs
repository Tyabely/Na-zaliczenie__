using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField]
    private int maxHealth = 3;
    private int currentHealth;

    [SerializeField]
    private ParticleSystem deathEffect;
    [SerializeField]
    private AudioClip deathSound;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Enemy hit! Health: {currentHealth}/{maxHealth}");

        // Sprawdü czy enemy umar≥
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy died!");

        // Efekt úmierci
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // DüwiÍk úmierci
        if (deathSound != null)
        {
            GameObject soundObject = new GameObject("EnemyDeathSound");
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.clip = deathSound;
            audioSource.volume = 0.7f;
            audioSource.Play();
            Destroy(soundObject, deathSound.length);
        }

        // Zniszcz enemy
        Destroy(gameObject);
    }

    // Opcjonalnie: metoda do leczenia
    public void Heal(int healAmount)
    {
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        Debug.Log($"Enemy healed! Health: {currentHealth}/{maxHealth}");
    }
}