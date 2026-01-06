using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerAction : MonoBehaviour
{
    [Header("Weapon References")]
    [SerializeField] private Rifle rifle;
    [SerializeField] private Gun gun;
    [SerializeField] private Shotgun shotgun;
    [SerializeField] private Camera playerCamera;

    [Header("UI Objects")]
    public GameObject RiflePng;
    public GameObject Rifle;
    public GameObject reloadpng;
    public GameObject Gun;
    public GameObject Shotgun;
    public GameObject GunPng;
    public GameObject ShotgunPng;

    [Header("Weapon Audio Clips")]
    [SerializeField] private AudioClip gunShotClip;
    [SerializeField] private AudioClip shotgunShotClip;
    [SerializeField] private AudioClip rifleShotClip;
    [SerializeField] private AudioClip gunReloadClip;
    [SerializeField] private AudioClip shotgunReloadClip;
    [SerializeField] private AudioClip rifleReloadClip;

    [Header("Shooting Settings")]
    private bool canShoot = true;
    private float shootDelay = 0.15f;

    [Header("Reload Settings")]
    private bool isReloading = false;
    private float currentReloadTime = 0f;
    private float gunReloadTime = 3.0f;
    private float shotgunReloadTime = 5.0f;
    private float rifleReloadTime = 2.5f;
    private float currentReloadDuration;
    private GameObject currentReloadSoundObject;

    [Header("Current Weapon")]
    private int currentWeaponType = 1;
    private MonoBehaviour currentWeaponScript;

    [Header("Aiming Settings")]
    [SerializeField] private float aimFOV = 40f;
    [SerializeField] private float normalFOV = 60f;
    private bool isAiming = false;

    void Start()
    {
        InitializeWeapons();
    }

    void InitializeWeapons()
    {
        FindWeaponsIfNull();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cameras.Length > 0)
                {
                    playerCamera = cameras[0];
                    Debug.LogWarning("Using non-main camera!");
                }
            }
        }

        AssignCameraToWeapons();
        AssignPlayerToWeapons();

        if (reloadpng != null)
            reloadpng.SetActive(false);

        SetActiveWeapon(1);
    }

    void FindWeaponsIfNull()
    {
        if (rifle == null)
        {
            rifle = GetComponentInChildren<Rifle>(true);
            if (rifle != null) Debug.Log("Found Rifle in children");
        }

        if (shotgun == null)
        {
            shotgun = GetComponentInChildren<Shotgun>(true);
            if (shotgun != null) Debug.Log("Found Shotgun in children");
        }

        if (gun == null)
        {
            gun = GetComponentInChildren<Gun>(true);
            if (gun != null) Debug.Log("Found Gun in children");
        }
    }

    void AssignCameraToWeapons()
    {
        if (playerCamera == null)
        {
            Debug.LogError("No camera found!");
            return;
        }

        Debug.Log($"Assigning camera: {playerCamera.name}");

        if (gun != null)
        {
            gun.SetCamera(playerCamera.transform);
            Debug.Log("Camera assigned to Gun");
        }

        if (shotgun != null)
        {
            shotgun.SetCamera(playerCamera.transform);
            Debug.Log("Camera assigned to Shotgun");
        }

        if (rifle != null)
        {
            rifle.SetCamera(playerCamera.transform);
            Debug.Log("Camera assigned to Rifle");
        }
    }

    void AssignPlayerToWeapons()
    {
        Transform playerTransform = this.transform;

        if (gun != null)
        {
            gun.SetPlayer(playerTransform);
            Debug.Log("Player assigned to Gun");
        }

        if (shotgun != null)
        {
            shotgun.SetPlayer(playerTransform);
            Debug.Log("Player assigned to Shotgun");
        }

        if (rifle != null)
        {
            rifle.SetPlayer(playerTransform);
            Debug.Log("Player assigned to Rifle");
        }
    }

    void Update()
    {
        HandleAiming();
        HandleWeaponSwitching();

        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartReload();
        }

        HandleReloadWithDeltaTime();
        HandleShooting();
    }

    void HandleAiming()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
            SetWeaponAiming(true);

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = aimFOV;
            }

            Debug.Log("Aiming enabled");
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isAiming = false;
            SetWeaponAiming(false);

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = normalFOV;
            }

            Debug.Log("Aiming disabled");
        }
    }

    void SetWeaponAiming(bool aiming)
    {
        switch (currentWeaponType)
        {
            case 1: // Pistolet
                if (gun != null) gun.SetAiming(aiming);
                break;
            case 2: // Strzelba
                if (shotgun != null) shotgun.SetAiming(aiming);
                break;
            case 3: // Karabin
                if (rifle != null) rifle.SetAiming(aiming);
                break;
        }
    }

    void HandleWeaponSwitching()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetActiveWeapon(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetActiveWeapon(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetActiveWeapon(3);
        }
    }

    void SetActiveWeapon(int weaponType)
    {
        if (isReloading)
        {
            Debug.Log("Cannot switch weapon while reloading!");
            return;
        }

        // Wyłącz celowanie poprzedniej broni
        if (isAiming)
        {
            SetWeaponAiming(false);
            isAiming = false;
            if (playerCamera != null)
                playerCamera.fieldOfView = normalFOV;
        }

        currentWeaponType = weaponType;

        // Aktywuj/Deaktywuj modele broni
        if (Gun != null) Gun.SetActive(weaponType == 1);
        if (Shotgun != null) Shotgun.SetActive(weaponType == 2);
        if (Rifle != null) Rifle.SetActive(weaponType == 3);

        // Aktywuj/Deaktywuj UI
        if (GunPng != null) GunPng.SetActive(weaponType == 1);
        if (ShotgunPng != null) ShotgunPng.SetActive(weaponType == 2);
        if (RiflePng != null) RiflePng.SetActive(weaponType == 3);

        // Ustaw aktualny skrypt broni
        currentWeaponScript = weaponType switch
        {
            1 => gun,
            2 => shotgun,
            3 => rifle,
            _ => gun
        };

        Debug.Log($"Switched to weapon: {weaponType}");

        // Ustaw opóźnienie strzału dla broni
        shootDelay = weaponType switch
        {
            1 => 0.5f,    // Pistolet
            2 => 0.5f,    // Strzelba
            3 => 0.15f,   // Karabin
            _ => 0.5f
        };
    }

    void HandleShooting()
    {
        if (!canShoot || isReloading) return;

        if (currentWeaponType == 3 && rifle != null && rifle.automaticFire)
        {
            if (Input.GetMouseButton(0))
            {
                if (rifle.BulletCount > 0 && !rifle.IsReloading())
                {
                    rifle.Shoot();
                    CreateRifleSound();
                    StartCoroutine(ShootDelay());
                }
                else if (rifle.BulletCount <= 0 && !isReloading)
                {
                    StartReload();
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            OnShoot();
        }
    }

    public void OnShoot()
    {
        if (!canShoot || isReloading) return;

        switch (currentWeaponType)
        {
            case 1:
                HandleGunShoot();
                break;
            case 2:
                HandleShotgunShoot();
                break;
            case 3:
                HandleRifleSingleShot();
                break;
        }
    }

    void HandleGunShoot()
    {
        if (gun == null) return;

        if (gun.BulletCount > 0 && gun.canShoot && !gun.isReloading)
        {
            gun.Shoot();
            CreateGunSound();
            StartCoroutine(ShootDelay());

            if (gun.BulletCount <= 0)
            {
                Debug.Log("Pistolet: brak amunicji!");
                TryAutoReload();
            }
        }
        else if (gun.isReloading)
        {
            Debug.Log("Pistolet: trwa przeładowanie!");
        }
        else
        {
            Debug.Log("Pistolet: pusty magazynek!");
            TryAutoReload();
        }
    }

    void HandleShotgunShoot()
    {
        if (shotgun == null) return;

        if (shotgun.BulletCount > 0 && shotgun.CanShoot())
        {
            shotgun.Shoot();
            CreateShotgunSound();
            StartCoroutine(ShootDelay());

            if (shotgun.BulletCount <= 0)
            {
                Debug.Log("Strzelba: brak amunicji!");
                TryAutoReload();
            }
        }
        else if (!shotgun.CanShoot())
        {
            Debug.Log("Strzelba: nie może strzelać!");
            TryAutoReload();
        }
        else
        {
            Debug.Log("Strzelba: pusty magazynek!");
            TryAutoReload();
        }
    }

    void HandleRifleSingleShot()
    {
        if (rifle == null || rifle.automaticFire) return;

        if (rifle.BulletCount > 0 && !rifle.IsReloading())
        {
            rifle.Shoot();
            CreateRifleSound();
            StartCoroutine(ShootDelay());

            if (rifle.BulletCount <= 0)
            {
                Debug.Log("Karabin: brak amunicji!");
                TryAutoReload();
            }
        }
        else if (rifle.IsReloading())
        {
            Debug.Log("Karabin: trwa przeładowanie!");
        }
        else
        {
            Debug.Log("Karabin: pusty magazynek!");
            TryAutoReload();
        }
    }

    void TryAutoReload()
    {
        if (!isReloading)
        {
            StartReload();
        }
    }

    IEnumerator ShootDelay()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootDelay);
        canShoot = true;
    }

    void StartReload()
    {
        bool needsReload = currentWeaponType switch
        {
            1 => gun != null && gun.BulletCount < gun.MaxBullets,
            2 => shotgun != null && shotgun.BulletCount < shotgun.MaxBullets,
            3 => rifle != null && rifle.BulletCount < rifle.MaxBullets,
            _ => false
        };

        if (!needsReload)
        {
            Debug.Log("Broń już ma pełny magazynek!");
            return;
        }

        if (reloadpng != null)
            reloadpng.SetActive(true);

        isReloading = true;
        currentReloadTime = 0f;

        switch (currentWeaponType)
        {
            case 1:
                currentReloadDuration = gunReloadTime;
                Debug.Log("Rozpoczęto przeładowanie pistoletu (3s)...");
                PlayGunReloadSound();
                break;
            case 2:
                currentReloadDuration = shotgunReloadTime;
                Debug.Log("Rozpoczęto przeładowanie strzelby (5s)...");
                PlayShotgunReloadSound();
                break;
            case 3:
                currentReloadDuration = rifleReloadTime;
                Debug.Log("Rozpoczęto przeładowanie karabinu (2.5s)...");
                PlayRifleReloadSound();
                break;
        }
    }

    void HandleReloadWithDeltaTime()
    {
        if (!isReloading) return;

        currentReloadTime += Time.deltaTime;

        float progress = Mathf.Clamp01(currentReloadTime / currentReloadDuration);
        UpdateReloadUI(progress);

        if (currentReloadTime >= currentReloadDuration)
        {
            CompleteReload();
        }
    }

    void CompleteReload()
    {
        if (reloadpng != null)
            reloadpng.SetActive(false);

        switch (currentWeaponType)
        {
            case 1:
                if (gun != null)
                {
                    gun.BulletCount = gun.MaxBullets;
                    UpdateWeaponAmmoUI(gun);
                }
                break;
            case 2:
                if (shotgun != null)
                {
                    shotgun.BulletCount = shotgun.MaxBullets;
                    UpdateWeaponAmmoUI(shotgun);
                }
                break;
            case 3:
                if (rifle != null)
                {
                    rifle.BulletCount = rifle.MaxBullets;
                    UpdateWeaponAmmoUI(rifle);
                }
                break;
        }

        isReloading = false;
        currentReloadTime = 0f;

        if (currentReloadSoundObject != null)
        {
            Destroy(currentReloadSoundObject);
            currentReloadSoundObject = null;
        }

        Debug.Log("Przeładowano broń!");
    }

    void UpdateWeaponAmmoUI(MonoBehaviour weapon)
    {
        if (weapon is Gun gunWeapon && gunWeapon.BulletsText != null)
        {
            gunWeapon.BulletsText.text = $"{gunWeapon.BulletCount} / {gunWeapon.MaxBullets}";
        }
        else if (weapon is Shotgun shotgunWeapon && shotgunWeapon.BulletsText != null)
        {
            shotgunWeapon.BulletsText.text = $"{shotgunWeapon.BulletCount} / {shotgunWeapon.MaxBullets}";
        }
        else if (weapon is Rifle rifleWeapon && rifleWeapon.BulletsText != null)
        {
            rifleWeapon.BulletsText.text = $"{rifleWeapon.BulletCount} / {rifleWeapon.MaxBullets}";
        }
    }

    void UpdateReloadUI(float progress)
    {
        // Możesz dodać pasek postępu lub inne UI
        // Aktualnie tylko włączamy/wyłączamy reloadpng
    }

    // METODY DŹWIĘKOWE
    void PlayGunReloadSound()
    {
        PlayReloadSound(gunReloadClip, "GunReloadSound", 0.6f);
    }

    void PlayShotgunReloadSound()
    {
        PlayReloadSound(shotgunReloadClip, "ShotgunReloadSound", 0.7f);
    }

    void PlayRifleReloadSound()
    {
        PlayReloadSound(rifleReloadClip, "RifleReloadSound", 0.6f);
    }

    void PlayReloadSound(AudioClip clip, string soundName, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning($"No reload sound for {soundName}");
            return;
        }

        currentReloadSoundObject = new GameObject(soundName);
        AudioSource audioSource = currentReloadSoundObject.AddComponent<AudioSource>();

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = 1.0f;
        audioSource.spatialBlend = 1.0f;
        audioSource.loop = false;

        audioSource.Play();
        Destroy(currentReloadSoundObject, clip.length + 0.5f);
    }

    void CreateGunSound()
    {
        CreateSoundEffect(gunShotClip, "GunSoundInstance", 0.7f, 0.95f, 1.05f);
    }

    void CreateShotgunSound()
    {
        CreateSoundEffect(shotgunShotClip, "ShotgunSoundInstance", 0.8f, 0.9f, 1.1f);
    }

    void CreateRifleSound()
    {
        CreateSoundEffect(rifleShotClip, "RifleSoundInstance", 0.8f, 0.95f, 1.05f);
    }

    void CreateSoundEffect(AudioClip clip, string soundName, float volume, float minPitch, float maxPitch)
    {
        if (clip == null)
        {
            Debug.LogWarning($"No shot sound for {soundName}");
            return;
        }

        GameObject soundObject = new GameObject(soundName);
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.spatialBlend = 1.0f;
        audioSource.maxDistance = 50f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        audioSource.Play();
        Destroy(soundObject, clip.length + 0.1f);
    }

    // METODY PUBLICZNE
    public bool CanShoot()
    {
        return canShoot && !isReloading;
    }

    public bool IsReloading()
    {
        return isReloading;
    }

    public int GetCurrentAmmo()
    {
        return currentWeaponType switch
        {
            1 => gun != null ? gun.BulletCount : 0,
            2 => shotgun != null ? shotgun.BulletCount : 0,
            3 => rifle != null ? rifle.BulletCount : 0,
            _ => 0
        };
    }

    public int GetMaxAmmo()
    {
        return currentWeaponType switch
        {
            1 => gun != null ? gun.MaxBullets : 0,
            2 => shotgun != null ? shotgun.MaxBullets : 0,
            3 => rifle != null ? rifle.MaxBullets : 0,
            _ => 0
        };
    }

    public int GetCurrentWeaponType()
    {
        return currentWeaponType;
    }

    public void AddAmmo(int weaponType, int amount)
    {
        switch (weaponType)
        {
            case 1:
                if (gun != null) gun.BulletCount = Mathf.Clamp(gun.BulletCount + amount, 0, gun.MaxBullets);
                break;
            case 2:
                if (shotgun != null) shotgun.BulletCount = Mathf.Clamp(shotgun.BulletCount + amount, 0, shotgun.MaxBullets);
                break;
            case 3:
                if (rifle != null) rifle.BulletCount = Mathf.Clamp(rifle.BulletCount + amount, 0, rifle.MaxBullets);
                break;
        }
    }

    public MonoBehaviour GetCurrentWeapon()
    {
        return currentWeaponType switch
        {
            1 => gun,
            2 => shotgun,
            3 => rifle,
            _ => null
        };
    }

    // Czyszczenie
    void OnDestroy()
    {
        if (currentReloadSoundObject != null)
            Destroy(currentReloadSoundObject);
    }

    // Debugowanie
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), $"Weapon: {currentWeaponType}");
        GUI.Label(new Rect(10, 30, 200, 20), $"Ammo: {GetCurrentAmmo()}/{GetMaxAmmo()}");
        GUI.Label(new Rect(10, 50, 200, 20), $"Can Shoot: {canShoot}");
        GUI.Label(new Rect(10, 70, 200, 20), $"Reloading: {isReloading}");
        GUI.Label(new Rect(10, 90, 200, 20), $"Aiming: {isAiming}");
    }
}