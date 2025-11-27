using UnityEngine;
using System.Collections;

public class SpawnEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject ZombiePrefab;
    public Transform[] spawnPoints;

    [Header("Spawn Settings")]
    public int enemiesPerWave = 10;
    public float spawnInterval = 2f;
    public float timeBetweenWaves = 10f;

    [Header("Current Stats")]
    public int currentWave = 0;
    public int enemiesSpawnedThisWave = 0;
    public int enemiesAlive = 0;

    private bool waveInProgress = false;
    private float waveCountdown;

    void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = new Transform[] { transform };
        }

        waveCountdown = timeBetweenWaves;
        Debug.Log("System spawnu przeciwników uruchomiony. Pierwsza fala za: " + waveCountdown + " sekund");
    }

    void Update()
    {
        if (!waveInProgress)
        {
            waveCountdown -= Time.deltaTime;

            if (waveCountdown <= 0f)
            {
                StartCoroutine(StartWave());
                waveCountdown = timeBetweenWaves;
            }
        }
    }

    IEnumerator StartWave()
    {
        waveInProgress = true;
        currentWave++;
        enemiesSpawnedThisWave = 0;

        Debug.Log("Rozpoczyna siê fala " + currentWave + "! Spawnowanie " + enemiesPerWave + " przeciwników.");

        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnZombie();
            enemiesSpawnedThisWave++;
            enemiesAlive++;

            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log("Zakoñczono spawnowanie fali " + currentWave + ". Pozosta³o przy ¿yciu: " + enemiesAlive + " przeciwników.");
    }

    void SpawnZombie()
    {
        if (ZombiePrefab == null)
        {
            Debug.LogError("Brak przypisanego prefaba zombie!");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject zombie = Instantiate(ZombiePrefab, spawnPoint.position, spawnPoint.rotation);
        zombie.transform.parent = transform;

        // POPRAWIONE: U¿yj event zamiast Action i +=
        EnemyHealth enemyHealth = zombie.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.OnEnemyDeath += OnEnemyDeath;
        }

        Debug.Log("Spawnowano zombie na pozycji: " + spawnPoint.position);
    }

    void OnEnemyDeath()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0 && enemiesSpawnedThisWave >= enemiesPerWave)
        {
            waveInProgress = false;
            Debug.Log("Fala " + currentWave + " zakoñczona! Nastêpna fala za: " + timeBetweenWaves + " sekund");

            enemiesPerWave = Mathf.RoundToInt(enemiesPerWave * 1.2f);
        }
    }

    // Dodatkowa metoda do czyszczenia eventów (opcjonalnie)
    void OnDestroy()
    {
        // Mo¿esz dodaæ czyszczenie eventów jeœli to konieczne
    }

    void OnDrawGizmos()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Gizmos.color = Color.red;
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                }
            }
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    public void SetSpawnRate(float newSpawnInterval)
    {
        spawnInterval = newSpawnInterval;
    }

    public void SetWaveSize(int newWaveSize)
    {
        enemiesPerWave = newWaveSize;
    }

    public void SkipWaveCountdown()
    {
        if (!waveInProgress)
        {
            waveCountdown = 0f;
        }
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    public int GetEnemiesAlive()
    {
        return enemiesAlive;
    }

    public float GetNextWaveCountdown()
    {
        return waveCountdown;
    }
}