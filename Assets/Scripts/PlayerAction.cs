using UnityEngine;
using System.Collections;

public class PlayerAction : MonoBehaviour
{
    [Header("Weapon References")]
    [SerializeField] private Rifle rifle;
    [SerializeField] private Gun gun;
    [SerializeField] private Shotgun shotgun;
    [SerializeField] private Camera playerCamera;

    [Header("UI Objects")]
    public GameObject RiflePng;
    public GameObject Rifle; // DODANO: Referencja do obiektu karabinu
    public GameObject reloadpng;
    public GameObject Gun;
    public GameObject Shotgun;
    public GameObject GunPng;
    public GameObject ShotgunPng;

    [Header("Weapon Audio Clips")]
    [SerializeField] private AudioClip gunShotClip;
    [SerializeField] private AudioClip shotgunShotClip;
    [SerializeField] private AudioClip rifleShotClip; // DODANO: Dźwięk karabinu
    [SerializeField] private AudioClip gunReloadClip;
    [SerializeField] private AudioClip shotgunReloadClip;
    [SerializeField] private AudioClip rifleReloadClip; // DODANO: Dźwięk przeładowania karabinu

    [Header("Shooting Settings")]
    private bool canShoot = true;
    private float shootDelay = 0.5f;

    [Header("Reload Settings")]
    private bool isReloading = false;
    private float currentReloadTime = 0f;
    private float gunReloadTime = 3.0f;
    private float shotgunReloadTime = 5.0f;
    private float rifleReloadTime = 2.5f; // DODANO: Czas przeładowania karabinu
    private float currentReloadDuration;
    private GameObject currentReloadSoundObject;

    void Start()
    {
        // Znajdź komponenty broni jeśli nie są przypisane
        if (rifle == null) // DODANO: Szukaj karabinu
        {
            rifle = GetComponentInChildren<Rifle>(true);
        }
        if (shotgun == null)
        {
            shotgun = GetComponentInChildren<Shotgun>(true);
        }
        if (gun == null)
        {
            gun = GetComponentInChildren<Gun>(true);
        }

        // Znajdź kamerę jeśli nie jest przypisana
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = Object.FindAnyObjectByType<Camera>();
            }
        }

        // Przekaż referencję kamery do broni
        if (gun != null && playerCamera != null)
            gun.SetCamera(playerCamera.transform);
        if (shotgun != null && playerCamera != null)
            shotgun.SetCamera(playerCamera.transform);
        if (rifle != null && playerCamera != null) // DODANO: Dla karabinu
            rifle.SetCamera(playerCamera.transform);

        // Ukryj reload UI na starcie
        if (reloadpng != null)
            reloadpng.SetActive(false);
    }

    void Update()
    {
        // Przełączanie broni (1 - pistolet, 2 - strzelba, 3 - karabin)
        HandleWeaponSwitching();

        // Przeładowanie po wciśnięciu R
        if (UnityEngine.Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartReload();
        }

        // Obsługa przeładowania z deltaTime
        HandleReloadWithDeltaTime();
    }

    private void HandleWeaponSwitching()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha1))
        {
            if (!isReloading)
            {
                SetActiveWeapon(1); // Pistolet
            }
        }
        else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha2))
        {
            if (!isReloading)
            {
                SetActiveWeapon(2); // Strzelba
            }
        }
        else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha3))
        {
            if (!isReloading)
            {
                SetActiveWeapon(3); // Karabin
            }
        }
    }

    private void SetActiveWeapon(int weaponType)
    {
        // 1 = pistolet, 2 = strzelba, 3 = karabin
        Gun.SetActive(weaponType == 1);
        GunPng.SetActive(weaponType == 1);
        Shotgun.SetActive(weaponType == 2);
        ShotgunPng.SetActive(weaponType == 2);
        Rifle.SetActive(weaponType == 3); // DODANO: Aktywacja karabinu
        RiflePng.SetActive(weaponType == 3);
    }

    public void OnShoot()
    {
        if (!canShoot || isReloading) return;

        if (Gun.activeInHierarchy)
        {
            HandleGunShoot();
        }
        else if (Shotgun.activeInHierarchy)
        {
            HandleShotgunShoot();
        }
        else if (Rifle.activeInHierarchy) // DODANO: Strzelanie z karabinu
        {
            HandleRifleShoot();
        }
    }

    private void HandleGunShoot()
    {
        int currentGunAmmo = GetGunAmmo();

        if (currentGunAmmo > 0)
        {
            gun.Shoot();
            CreateGunSound();
            StartCoroutine(ShootDelay());

            if (GetGunAmmo() <= 0)
            {
               // Debug.Log("Pistolet: brak amunicji!");
            }
        }
        else
        {
          //  Debug.Log("Pistolet: pusty magazynek!");
        }
    }

    private void HandleShotgunShoot()
    {
        if (shotgun != null)
        {
            int currentShotgunAmmo = GetShotgunAmmo();

            if (currentShotgunAmmo > 0)
            {
                StartCoroutine(ShootShotgunBurst());
            }
            else
            {
              //  Debug.Log("Strzelba: brak amunicji!");
            }
        }
        else
        {
          //  Debug.LogError("Shotgun reference is null!");
        }
    }

    private IEnumerator ShootShotgunBurst()
    {
        if (!canShoot) yield break;

        canShoot = false;

        // Strzelba strzela serią (3 pociski na raz)
        for (int i = 0; i < 3; i++)
        {
            shotgun.Shoot();
            yield return new WaitForSeconds(0.1f);
        }

        if (GetShotgunAmmo() <= 0)
        {
           // Debug.Log("Strzelba: brak amunicji!");
        }

        yield return new WaitForSeconds(shootDelay);
        canShoot = true;
    }

    private void HandleRifleShoot() // DODANO: Metoda strzelania karabinem
    {
        int currentRifleAmmo = GetRifleAmmo();

        if (currentRifleAmmo > 0)
        {
            rifle.Shoot();
            CreateRifleSound();
            StartCoroutine(ShootDelay());

            if (GetRifleAmmo() <= 0)
            {
              //  Debug.Log("Karabin: brak amunicji!");
            }
        }
        else
        {
          //  Debug.Log("Karabin: pusty magazynek!");
        }
    }

    private IEnumerator ShootDelay()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootDelay);
        canShoot = true;
    }

    private void HandleReloadWithDeltaTime()
    {
        if (isReloading)
        {
            currentReloadTime += Time.deltaTime;

            // Aktualizuj postęp w konsoli
            float progress = currentReloadTime / currentReloadDuration;
            UpdateReloadUI(progress);

            // Sprawdź czy przeładowanie zakończone
            if (currentReloadTime >= currentReloadDuration)
            {
                CompleteReload();
                isReloading = false;
                currentReloadTime = 0f;

                // Zatrzymaj dźwięk przeładowania
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
        if (reloadpng != null)
            reloadpng.SetActive(true);

        isReloading = true;
        currentReloadTime = 0f;

        // Ustaw odpowiedni czas przeładowania w zależności od aktywnej broni
        if (Gun.activeInHierarchy)
        {
            currentReloadDuration = gunReloadTime;
           // Debug.Log("Rozpoczęto przeładowanie pistoletu (3s)...");
            PlayGunReloadSound();
        }
        else if (Shotgun.activeInHierarchy)
        {
            currentReloadDuration = shotgunReloadTime;
           // Debug.Log("Rozpoczęto przeładowanie strzelby (5s)...");
            PlayShotgunReloadSound();
        }
        else if (Rifle.activeInHierarchy) // DODANO: Przeładowanie karabinu
        {
            currentReloadDuration = rifleReloadTime;
           // Debug.Log("Rozpoczęto przeładowanie karabinu (2.5s)...");
            PlayRifleReloadSound();
        }
        else
        {
            // Jeśli żadna broń nie jest aktywna, przeładuj domyślnie pistolet
            currentReloadDuration = gunReloadTime;
           // Debug.Log("Rozpoczęto przeładowanie (3s)...");
            PlayGunReloadSound();
        }
    }

    private void CompleteReload()
    {
        if (reloadpng != null)
            reloadpng.SetActive(false);

        // Przeładuj odpowiednią broń
        if (Gun.activeInHierarchy && gun != null)
        {
            gun.BulletCount = gun.MaxBullets;
            if (gun.BulletsText != null)
                gun.BulletsText.text = gun.BulletCount.ToString() + " / " + gun.MaxBullets.ToString();
        }
        else if (Shotgun.activeInHierarchy && shotgun != null)
        {
            shotgun.BulletCount = shotgun.MaxBullets;
            if (shotgun.BulletsText != null)
                shotgun.BulletsText.text = shotgun.BulletCount.ToString() + " / " + shotgun.MaxBullets.ToString();
        }
        else if (Rifle.activeInHierarchy && rifle != null) // DODANO: Przeładowanie karabinu
        {
            // Użyj metody Reload z karabinu jeśli istnieje, lub ustaw bezpośrednio
            rifle.BulletCount = rifle.MaxBullets;
            if (rifle.BulletsText != null)
                rifle.BulletsText.text = rifle.BulletCount.ToString() + " / " + rifle.MaxBullets.ToString();
        }

        //Debug.Log("Przeładowano broń!");
    }

    private void UpdateReloadUI(float progress)
    {
        // Możesz dodać pasek postępu w UI
        //Debug.Log($"Przeładowanie: {progress * 100:F1}%");
    }

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

            // Zniszcz po zakończeniu dźwięku
            Destroy(currentReloadSoundObject, gunReloadClip.length + 1f);
        }
    }

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

            // Zniszcz po zakończeniu dźwięku
            Destroy(currentReloadSoundObject, shotgunReloadClip.length + 1f);
        }
    }

    private void PlayRifleReloadSound() // DODANO: Dźwięk przeładowania karabinu
    {
        if (rifleReloadClip != null)
        {
            currentReloadSoundObject = new GameObject("RifleReloadSound");
            AudioSource audioSource = currentReloadSoundObject.AddComponent<AudioSource>();

            audioSource.clip = rifleReloadClip;
            audioSource.volume = 0.6f;
            audioSource.pitch = 1.0f;
            audioSource.spatialBlend = 1.0f;
            audioSource.loop = false;

            audioSource.Play();

            // Zniszcz po zakończeniu dźwięku
            Destroy(currentReloadSoundObject, rifleReloadClip.length + 1f);
        }
        else if (gunReloadClip != null) // Fallback na dźwięk pistoletu
        {
            PlayGunReloadSound();
        }
    }

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

    private void CreateRifleSound() // DODANO: Dźwięk strzału karabinu
    {
        if (rifleShotClip != null)
        {
            GameObject soundObject = new GameObject("RifleSoundInstance");
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();

            audioSource.clip = rifleShotClip;
            audioSource.volume = 0.8f; // Nieco głośniejszy niż pistolet
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.spatialBlend = 1.0f;
            audioSource.maxDistance = 50f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

            audioSource.Play();
            Destroy(soundObject, rifleShotClip.length + 0.1f);
        }
        else if (gunShotClip != null) // Fallback na dźwięk pistoletu
        {
            CreateGunSound();
        }
    }

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

    private int GetRifleAmmo() // DODANO: Pobieranie amunicji karabinu
    {
        if (rifle != null) return rifle.BulletCount;
        return 0;
    }

    // Czyszczenie
    private void OnDestroy()
    {
        if (currentReloadSoundObject != null)
            Destroy(currentReloadSoundObject);
    }

    // Metoda do sprawdzenia czy można strzelać (dla innych skryptów)
    public bool CanShoot()
    {
        return canShoot && !isReloading;
    }

    // Metoda do sprawdzenia czy trwa przeładowanie (dla innych skryptów)
    public bool IsReloading()
    {
        return isReloading;
    }
}