using UnityEngine;
using System.Collections;

public class PlayerAction : MonoBehaviour
{
    [SerializeField]
    private Gun gun;
    [SerializeField]
    private Shotgun shotgun;
    public GameObject reloadpng;
    public GameObject Gun;
    public GameObject Shotgun;
    public GameObject GunPng;
    public GameObject ShotgunPng;

    // AudioClips tylko dla broni
    [SerializeField]
    private AudioClip gunShotClip;
    [SerializeField]
    private AudioClip shotgunShotClip;
    [SerializeField]
    private AudioClip gunReloadClip;    // DŸwiêk prze³adowania pistoletu
    [SerializeField]
    private AudioClip shotgunReloadClip; // DŸwiêk prze³adowania strzelby

    private bool canShoot = true;
    private float shootDelay = 0.5f;

    // Zmienne do prze³adowania
    private bool isReloading = false;
    private float currentReloadTime = 0f;
    private float gunReloadTime = 3.0f;    // 3 sekundy dla pistoletu
    private float shotgunReloadTime = 5.0f; // 5 sekund dla strzelby
    private float currentReloadDuration;
    private GameObject currentReloadSoundObject;

    void Start()
    {
        if (shotgun == null)
        {
            shotgun = GetComponentInChildren<Shotgun>(true);
        }
        if (gun == null)
        {
            gun = GetComponentInChildren<Gun>(true);
        }
    }

    public void Update()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha1))
        {
            if (!isReloading)
            {
                Gun.SetActive(true);
                GunPng.SetActive(true);
                ShotgunPng.SetActive(false);
                Shotgun.SetActive(false);
            }
        }
        else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha2))
        {
            if (!isReloading)
            {
                Shotgun.SetActive(true);
                ShotgunPng.SetActive(true);
                GunPng.SetActive(false);
                Gun.SetActive(false);
            }
        }

        // Prze³adowanie po wciœniêciu R - ZMIENIONE: zawsze wywo³uje siê po klikniêciu R
        if (UnityEngine.Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartReload();
        }

        // Obs³uga prze³adowania z deltaTime
        HandleReloadWithDeltaTime();
    }

    public void OnShoot()
    {
        if (!canShoot || isReloading) return; // Blokada podczas prze³adowania

        if (Gun.activeInHierarchy)
        {
            // Pobierz aktualn¹ amunicjê z pistoletu
            int currentGunAmmo = GetGunAmmo();

            if (currentGunAmmo > 0)
            {
                gun.Shoot();
                CreateGunSound();
                StartCoroutine(ShootDelay());

                if (GetGunAmmo() <= 0)
                {
                    Debug.Log("Pistolet: brak amunicji!");
                }
            }
            else
            {
                Debug.Log("Pistolet: pusty magazynek!");
            }
        }
        else if (Shotgun.activeInHierarchy)
        {
            if (shotgun != null)
            {
                // Pobierz aktualn¹ amunicjê ze strzelby
                int currentShotgunAmmo = GetShotgunAmmo();

                if (currentShotgunAmmo > 0)
                {
                    StartCoroutine(ShootShotgunBurst());
                }
                else
                {
                    Debug.Log("Strzelba: brak amunicji!");
                }
            }
            else
            {
                Debug.LogError("Shotgun reference is null!");
            }
        }
    }

    private IEnumerator ShootShotgunBurst()
    {
        if (!canShoot) yield break;

        canShoot = false;

        // Strzelba strzela seri¹ (3 pociski na raz)
        for (int i = 0; i < 3; i++)
        {
            shotgun.Shoot(); // DŸwiêk jest teraz odtwarzany wewn¹trz metody Shoot() strzelby
                             // USUÑ: CreateShotgunSound(); - dŸwiêk jest ju¿ odtwarzany w shotgun.Shoot()
            yield return new WaitForSeconds(0.1f);
        }

        if (GetShotgunAmmo() <= 0)
        {
            Debug.Log("Strzelba: brak amunicji!");
        }

        yield return new WaitForSeconds(shootDelay);
        canShoot = true;
    }

    private IEnumerator ShootDelay()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootDelay);
        canShoot = true;
    }

    // Obs³uga prze³adowania z deltaTime
    private void HandleReloadWithDeltaTime()
    {
        if (isReloading)
        {
            currentReloadTime += Time.deltaTime;

            // Aktualizuj postêp w konsoli
            float progress = currentReloadTime / currentReloadDuration;
            UpdateReloadUI(progress);

            // SprawdŸ czy prze³adowanie zakoñczone
            if (currentReloadTime >= currentReloadDuration)
            {
                CompleteReload();
                isReloading = false;
                currentReloadTime = 0f;

                // Zatrzymaj dŸwiêk prze³adowania
                if (currentReloadSoundObject != null)
                {
                    Destroy(currentReloadSoundObject);
                    currentReloadSoundObject = null;
                }
            }
        }
    }

    private void StartReload()
    {
        reloadpng.SetActive(true);
        isReloading = true;
        currentReloadTime = 0f;

        // Ustaw odpowiedni czas prze³adowania w zale¿noœci od aktywnej broni
        if (Gun.activeInHierarchy)
        {
            currentReloadDuration = gunReloadTime;
            Debug.Log("Rozpoczêto prze³adowanie pistoletu (3s)...");
            PlayGunReloadSound();
        }
        else if (Shotgun.activeInHierarchy)
        {
            currentReloadDuration = shotgunReloadTime;
            Debug.Log("Rozpoczêto prze³adowanie strzelby (5s)...");
            PlayShotgunReloadSound();
        }
        else
        {
            // Jeœli ¿adna broñ nie jest aktywna, prze³aduj domyœlnie pistolet
            currentReloadDuration = gunReloadTime;
            Debug.Log("Rozpoczêto prze³adowanie (3s)...");
            PlayGunReloadSound();
        }
    }

    private void CompleteReload()
    {
        reloadpng.SetActive(false);
        // Prze³aduj obie bronie
        if (gun != null)
        {
            gun.BulletCount = gun.MaxBullets;
            if (gun.BulletsText != null)
                gun.BulletsText.text = gun.BulletCount.ToString() + " / " + gun.MaxBullets.ToString();
        }

        if (shotgun != null)
        {
            shotgun.BulletCount = shotgun.MaxBullets;
            if (shotgun.BulletsText != null)
                shotgun.BulletsText.text = shotgun.BulletCount.ToString() + " / " + shotgun.MaxBullets.ToString();
        }

        Debug.Log("Prze³adowano broñ!");
    }

    private void UpdateReloadUI(float progress)
    {
        // Mo¿esz dodaæ pasek postêpu w UI
        Debug.Log($"Prze³adowanie: {progress * 100:F1}%");
    }

    // DŸwiêk prze³adowania pistoletu
    private void PlayGunReloadSound()
    {
        if (gunReloadClip != null)
        {
            currentReloadSoundObject = new GameObject("GunReloadSound");
            AudioSource audioSource = currentReloadSoundObject.AddComponent<AudioSource>();

            audioSource.clip = gunReloadClip;
            audioSource.volume = 0.6f;
            audioSource.pitch = 1.0f;
            audioSource.spatialBlend = 1.0f;
            audioSource.loop = false;

            audioSource.Play();

            // Zniszcz po zakoñczeniu dŸwiêku
            Destroy(currentReloadSoundObject, gunReloadClip.length + 1f);
        }
    }

    // DŸwiêk prze³adowania strzelby
    private void PlayShotgunReloadSound()
    {
        if (shotgunReloadClip != null)
        {
            currentReloadSoundObject = new GameObject("ShotgunReloadSound");
            AudioSource audioSource = currentReloadSoundObject.AddComponent<AudioSource>();

            audioSource.clip = shotgunReloadClip;
            audioSource.volume = 0.7f;
            audioSource.pitch = 1.0f;
            audioSource.spatialBlend = 1.0f;
            audioSource.loop = false;

            audioSource.Play();

            // Zniszcz po zakoñczeniu dŸwiêku
            Destroy(currentReloadSoundObject, shotgunReloadClip.length + 1f);
        }
    }

    // Tworzy GameObject z dŸwiêkiem pistoletu
    private void CreateGunSound()
    {
        if (gunShotClip != null)
        {
            GameObject soundObject = new GameObject("GunSoundInstance");
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();

            audioSource.clip = gunShotClip;
            audioSource.volume = 0.7f;
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.spatialBlend = 1.0f;
            audioSource.maxDistance = 50f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

            audioSource.Play();
            Destroy(soundObject, gunShotClip.length + 0.1f);
        }
    }

    // Tworzy GameObject z dŸwiêkiem strzelby
    private void CreateShotgunSound()
    {
        if (shotgunShotClip != null)
        {
            GameObject soundObject = new GameObject("ShotgunSoundInstance");
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();

            audioSource.clip = shotgunShotClip;
            audioSource.volume = 0.8f;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.spatialBlend = 1.0f;
            audioSource.maxDistance = 50f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

            audioSource.Play();
            Destroy(soundObject, shotgunShotClip.length + 0.1f);
        }
    }

    // Metody do pobierania amunicji z broni
    private int GetGunAmmo()
    {
        if (gun != null) return gun.BulletCount;
        return 0;
    }

    private int GetShotgunAmmo()
    {
        if (shotgun != null) return shotgun.BulletCount;
        return 0;
    }

    // Czyszczenie
    private void OnDestroy()
    {
        if (currentReloadSoundObject != null) Destroy(currentReloadSoundObject);
    }
}