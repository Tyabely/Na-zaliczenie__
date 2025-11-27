using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    // DODAJ TÊ LINIÊ - zdarzenie œmierci przeciwnika
    public event Action OnEnemyDeath;

    [Header("Visual Effects")]
    public GameObject deathEffect;
    public AudioClip deathSound;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Wywo³aj zdarzenie œmierci PRZED zniszczeniem obiektu
        OnEnemyDeath?.Invoke();

        // Efekt œmierci
       /* if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }

        // DŸwiêk œmierci
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
*/
        // Zniszcz obiekt
        Destroy(gameObject);
    }

    // Metoda pomocnicza do sprawdzenia czy przeciwnik ¿yje
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}