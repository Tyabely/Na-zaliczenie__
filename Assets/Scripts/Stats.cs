using UnityEngine;

public class Stats : MonoBehaviour
{
    public GameObject PlayerController;
    [Header("Health")]
    public int damage = 1;
    public int maxHealth = 3;
    public int currentHealth;
    public HealthBar healthBar;

    [Header("Stamina")]
    public int MaxStamina = 100;
    public float CurrentStamina;
    public StaminaBar StaminaBar;
    public float staminaDepletionRate = 20f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1f;

    private bool isMoving = false;
    private bool isRunning = false;
    private float lastRunningTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
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
            Debug.Log("StaminaBar initialized!");
        }
        else
        {
            Debug.LogError("StaminaBar reference is not set in Inspector!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleStamina();
        isMoving = PlayerController.GetComponent<PlayerController>().isMoving;
        isRunning = PlayerController.GetComponent<PlayerController>().isRunning;

        // Tymczasowy test damage - usuñ póŸniej
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakenDamage(1);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Heal(1);
        }
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift))
        {
            DepleteStamina(1f);
        }
    }
    void HandleStamina()
    {
        if (isRunning && isMoving)
        {
            // Zu¿ycie staminy podczas biegania
            float depletionAmount = staminaDepletionRate * Time.deltaTime;
            DepleteStamina(depletionAmount);
            lastRunningTime = Time.time;
        }
        else if (CurrentStamina < MaxStamina && Time.time - lastRunningTime > staminaRegenDelay)
        {
            // Regeneracja staminy gdy nie biegasz przez okreœlony czas
            float regenAmount = staminaRegenRate * Time.deltaTime;
            RegenerateStamina(regenAmount);
        }
    }
    public void TakenDamage(int damage)
    {
        currentHealth -= damage;

        // Zabezpieczenie przed ujemnym zdrowiem
        if (currentHealth < 0) currentHealth = 0;

        // Aktualizacja health bara
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        Debug.Log($"Player took {damage} damage. Current health: {currentHealth}");

        // Sprawdzenie œmierci
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        currentHealth += healAmount;

        // Zabezpieczenie przed przekroczeniem maksymalnego zdrowia
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        // Aktualizacja health bara
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        Debug.Log($"Player healed {healAmount}. Current health: {currentHealth}");
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
        else
        {
            Debug.LogError("StaminaBar is null in DepleteStamina!");
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
        else
        {
            Debug.LogError("StaminaBar is null in RegenerateStamina!");
        }
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // Tutaj dodaj kod na umiera gracza
        // np. restart poziomu, game over screen, etc.
    }
}
