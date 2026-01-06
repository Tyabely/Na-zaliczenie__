using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // DODANE: To naprawia błąd IEnumerator
using TMPro; // Dodaj jeśli używasz TextMeshPro

public class Stats : MonoBehaviour
{
    public GameObject PlayerController;

    [Header("Health")]
    public int damage = 1;
    public int maxHealth = 10; // Zwiększyłem na 10
    public int currentHealth;
    public HealthBar healthBar;

    [Header("Stamina")]
    public int MaxStamina = 100;
    public float CurrentStamina;
    public StaminaBar StaminaBar;
    public float staminaDepletionRate = 20f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1f;

    [Header("Game Over")]
    public GameObject gameOverScreen;
    public string gameOverScene = "GameOver";
    public float deathDelay = 2f; // Czas przed game over

    [Header("Damage Effects")]
    public AudioClip damageSound;
    public AudioClip deathSound;
    private AudioSource audioSource;

    private bool isMoving = false;
    private bool isRunning = false;
    private float lastRunningTime;
    private bool isDead = false;
    private float lastDamageTime;
    public float damageCooldown = 0.5f; // Immunity frames

    void Start()
    {
        currentHealth = maxHealth;

        // Inicjalizacja health bar
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
        else
        {
            Debug.LogWarning("HealthBar reference is not set in Inspector!");
        }

        // Inicjalizacja staminy
        CurrentStamina = MaxStamina;
        if (StaminaBar != null)
        {
            StaminaBar.SetMaxStamina(MaxStamina);
            StaminaBar.SetStamina((int)CurrentStamina);
        }

        // Audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Upewnij się że gracz nie jest martwy na starcie
        isDead = false;
        Time.timeScale = 1f; // Upewnij się że czas płynie normalnie
    }

    void Update()
    {
        if (isDead) return;

        HandleStamina();

        // Pobierz komponenty PlayerController jeśli są dostępne
        if (PlayerController != null)
        {
            PlayerController playerController = PlayerController.GetComponent<PlayerController>();
            if (playerController != null)
            {
                isMoving = playerController.isMoving;
                isRunning = playerController.isRunning;
            }
        }

        // Testowe klawisze (możesz usunąć)
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakenDamage(1);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Heal(1);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            Die();
        }
    }

    void HandleStamina()
    {
        if (isRunning && isMoving && CurrentStamina > 0)
        {
            float depletionAmount = staminaDepletionRate * Time.deltaTime;
            DepleteStamina(depletionAmount);
            lastRunningTime = Time.time;
        }
        else if (CurrentStamina < MaxStamina && Time.time - lastRunningTime > staminaRegenDelay)
        {
            float regenAmount = staminaRegenRate * Time.deltaTime;
            RegenerateStamina(regenAmount);
        }
    }

    // GŁÓWNA METODA DLA ODBIERANIA OBRAŻEŃ
    public void TakeDamageFromEnemy(int damageAmount)
    {
        if (isDead || Time.time - lastDamageTime < damageCooldown)
            return;

        lastDamageTime = Time.time;
        TakenDamage(damageAmount);

        // Dźwięk obrażeń
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        Debug.Log($"Player took {damageAmount} damage! Health: {currentHealth}");
    }

    public void TakenDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // Aktualizacja health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        // Sprawdź śmierć
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        Debug.Log($"Player healed {healAmount}. Health: {currentHealth}");
    }

    // METODY STAMINY
    public void DepleteStamina(float amount)
    {
        CurrentStamina -= amount;
        CurrentStamina = Mathf.Max(0, CurrentStamina);

        if (StaminaBar != null)
        {
            StaminaBar.SetStamina((int)CurrentStamina);
        }
    }

    public void RegenerateStamina(float amount)
    {
        CurrentStamina += amount;
        CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina);

        if (StaminaBar != null)
        {
            StaminaBar.SetStamina((int)CurrentStamina);
        }
    }

    // METODA ŚMIERCI
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("PLAYER DIED!");

        // Dźwięk śmierci
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // Wyłącz kontrolę gracza
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this && script.enabled)
            {
                script.enabled = false;
            }
        }

        // Wyłącz Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Uruchom Game Over
        StartCoroutine(GameOverSequence());
    }

    // SEKWENCJA GAME OVER
    private IEnumerator GameOverSequence()
    {
        // Czekaj chwilę przed akcjami
        yield return new WaitForSeconds(0.5f);

        // Opcja 1: Pokazanie UI Game Over
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
            yield return new WaitForSeconds(deathDelay);

            // Restart sceny
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        // Opcja 2: Ładowanie sceny Game Over
        else if (!string.IsNullOrEmpty(gameOverScene))
        {
            yield return new WaitForSeconds(deathDelay);
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameOverScene);
        }
        // Opcja 3: Restart obecnej sceny
        else
        {
            yield return new WaitForSeconds(deathDelay);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // SPRAWDŹ CZY GRACZ ŻYJE
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0;
    }

    // KOLIZJE Z PRZECIWNIKAMI
    void OnCollisionEnter(Collision collision)
    {
        HandleEnemyCollision(collision.gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        HandleEnemyCollision(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleEnemyCollision(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        HandleEnemyCollision(other.gameObject);
    }

    void HandleEnemyCollision(GameObject other)
    {
        if (isDead) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyMovement enemyMovement = other.GetComponent<EnemyMovement>();

            int damageAmount = 1; // Domyślne obrażenia

            if (enemyMovement != null)
            {
                damageAmount = enemyMovement.damageAmount;
            }

            // Zadaj obrażenia
            TakeDamageFromEnemy(damageAmount);

            // Odepchnij gracza (opcjonalnie)
            Rigidbody playerRb = GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 pushDirection = (transform.position - other.transform.position).normalized;
                playerRb.AddForce(pushDirection * 5f, ForceMode.Impulse);
            }
        }
    }

    // PUBLICZNE METODY DOSTĘPOWE
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
    }
}