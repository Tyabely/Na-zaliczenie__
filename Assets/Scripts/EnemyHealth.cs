using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] public int maxHealth = 3;
    [SerializeField] public int currentHealth;

    [Header("Effects")]
    [SerializeField] private ParticleSystem deathEffect;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip hurtSound;

    // Publiczne w³aœciwoœci do dostêpu z innych skryptów
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Enemy hit! Health: {currentHealth}/{maxHealth}");

        // Odtwórz dŸwiêk otrzymania obra¿eñ
        if (hurtSound != null)
        {
            PlaySound(hurtSound);
        }

        // SprawdŸ czy enemy umar³
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log("Enemy died!");

        // Efekt œmierci
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // DŸwiêk œmierci
        if (deathSound != null)
        {
            PlaySound(deathSound);
        }

        // Zniszcz enemy
        Destroy(gameObject);
    }

    // Metoda do odtwarzania dŸwiêków
    private void PlaySound(AudioClip clip)
    {
        GameObject soundObject = new GameObject("EnemySound");
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = 0.7f;
        audioSource.Play();
        Destroy(soundObject, clip.length);
    }

    // Metoda do leczenia
    public void Heal(int healAmount)
    {
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        Debug.Log($"Enemy healed! Health: {currentHealth}/{maxHealth}");
    }

    // Metoda do ustawienia maksymalnego zdrowia
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
}