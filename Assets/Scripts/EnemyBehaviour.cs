using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private EnemyHealth enemyHealth;

    private void Start()
    {
        // Pobierz komponent EnemyHealth z tego samego obiektu
        enemyHealth = GetComponent<EnemyHealth>();

        // Jeœli nie ma EnemyHealth, dodaj go automatycznie
        if (enemyHealth == null)
        {
            enemyHealth = gameObject.AddComponent<EnemyHealth>();
            Debug.LogWarning("EnemyHealth component was missing and has been added automatically to " + gameObject.name);
        }
    }

    // Publiczna metoda Die, która mo¿e byæ wywo³ana z innych skryptów
    public void Die()
    {
        // ZnajdŸ komponent EnemyHealth jeœli nie zosta³ znaleziony w Start
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        // Jeœli EnemyHealth istnieje, u¿yj jego metody Die
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(enemyHealth.maxHealth); // Zadaj obra¿enia równe maksymalnemu zdrowiu
        }
        else
        {
            // Awaryjne zniszczenie obiektu jeœli nie ma EnemyHealth
            Debug.LogWarning("Enemy destroyed without EnemyHealth component");
            Destroy(gameObject);
        }
    }

    // Metoda do zadawania obra¿eñ temu przeciwnikowi
    public void TakeDamage(int damage)
    {
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
    }

    // Opcjonalnie: metoda do sprawdzania czy wróg ¿yje
    public bool IsAlive()
    {
        return enemyHealth != null && enemyHealth.currentHealth > 0;
    }
}