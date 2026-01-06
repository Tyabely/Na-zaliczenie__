using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class Shotgun : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource shotgunSound;
    [SerializeField] private AudioClip reloadSound;

    [Header("Shooting Settings")]
    [SerializeField] private bool addBulletSpread = true;
    [SerializeField] private Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] private float shootDelay = 0.5f;
    [SerializeField] private LayerMask mask = -1;
    [SerializeField] private float bulletSpeed = 100;
    [SerializeField] private int pelletCount = 6;
    [SerializeField] private int damagePerPellet = 25;

    public bool automaticFire = false;

    [Header("Aiming Settings")]
    [SerializeField] private bool canAim = true;
    [SerializeField] private Vector3 hipOffset = new Vector3(0.4f, -0.25f, 0.6f);
    [SerializeField] private Vector3 aimOffset = new Vector3(0f, -0.15f, 0.5f);
    [SerializeField] private float aimSpeed = 7f;
    [SerializeField] private float aimFOV = 50f;
    [SerializeField] private float hipFOV = 60f;
    private bool isAiming = false;
    private Camera weaponCamera;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem shootingSystem;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private ParticleSystem impactParticleSystem;
    [SerializeField] private TrailRenderer bulletTrail;

    [Header("Hitmarker")]
    [SerializeField] private Image hitmarkerImage;
    [SerializeField] private float hitmarkerDisplayTime = 0.2f;

    [Header("Ammo")]
    public int BulletCount;
    public int MaxBullets = 2;
    public TextMeshProUGUI BulletsText;
    public GameObject BulletCounter;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private Animator animator;
    private float lastShootTime;
    private bool canShoot = true;
    private bool isReloading = false;
    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        InitializeShotgun();
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleAiming();
        HandleInput();

        // JEDYNA METODA POZYCJONUJ¥CA
        UpdateWeaponPosition();

        UpdateAmmoUI();
    }

    private void InitializeShotgun()
    {
        BulletCount = MaxBullets;
        currentOffset = hipOffset;
        targetOffset = hipOffset;

        if (hitmarkerImage != null)
            hitmarkerImage.enabled = false;

        if (cameraTransform == null)
        {
            FindCamera();
        }

        weaponCamera = GetComponentInChildren<Camera>();
        if (weaponCamera == null && cameraTransform != null)
        {
            weaponCamera = cameraTransform.GetComponent<Camera>();
        }

        if (cameraTransform != null)
        {
            SetInitialPosition();
        }
    }

    private void SetInitialPosition()
    {
        Vector3 desiredPosition = cameraTransform.position +
                                 cameraTransform.right * hipOffset.x +
                                 cameraTransform.up * hipOffset.y +
                                 cameraTransform.forward * hipOffset.z;

        transform.position = desiredPosition;
        transform.rotation = cameraTransform.rotation;
    }

    private void UpdateWeaponPosition()
    {
        if (cameraTransform == null) return;

        currentOffset = Vector3.SmoothDamp(
            currentOffset,
            targetOffset,
            ref velocity,
            0.1f,
            aimSpeed
        );

        Vector3 desiredPosition = cameraTransform.position +
                                 cameraTransform.right * currentOffset.x +
                                 cameraTransform.up * currentOffset.y +
                                 cameraTransform.forward * currentOffset.z;

        transform.position = desiredPosition;
        transform.rotation = cameraTransform.rotation;
    }

    private void HandleAiming()
    {
        if (!canAim || cameraTransform == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
            addBulletSpread = false;
            bulletSpreadVariance = new Vector3(0.05f, 0.05f, 0.05f);
            targetOffset = aimOffset;

            if (weaponCamera != null)
            {
                weaponCamera.fieldOfView = aimFOV;
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isAiming = false;
            addBulletSpread = true;
            bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);
            targetOffset = hipOffset;

            if (weaponCamera != null)
            {
                weaponCamera.fieldOfView = hipFOV;
            }
        }
    }

    private void HandleInput()
    {
        if (isReloading || cameraTransform == null) return;

        if (automaticFire)
        {
            if (Input.GetMouseButton(0) && canShoot && BulletCount > 0)
            {
                if (Time.time - lastShootTime > shootDelay)
                {
                    Shoot();
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && canShoot && BulletCount > 0)
            {
                Shoot();
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && BulletCount < MaxBullets)
        {
            StartCoroutine(Reload());
        }

        if (BulletCount <= 0 && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    private void FindCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
        else
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 0)
            {
                cameraTransform = cameras[0].transform;
            }
        }
    }

    // USUNIÊTA METODA FollowCamera() - powodowa³a konflikt

    public void Shoot()
    {
        if (Time.time - lastShootTime < shootDelay || !canShoot || isReloading || BulletCount <= 0)
            return;

        BulletCount--;
        lastShootTime = Time.time;
        UpdateAmmoUI();

        if (shotgunSound != null)
            shotgunSound.Play();

        if (shootingSystem != null)
            shootingSystem.Play();

        bool enemyHit = false;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 direction = GetDirection();

            if (Physics.Raycast(bulletSpawnPoint.position, direction, out RaycastHit hit, float.MaxValue, mask))
            {
                TrailRenderer trail = Instantiate(bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, true));

                if (hit.collider.CompareTag("Enemy"))
                {
                    enemyHit = true;

                    EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                    if (enemyHealth == null)
                    {
                        enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
                    }

                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damagePerPellet);
                    }
                }
            }
            else
            {
                TrailRenderer trail = Instantiate(bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, bulletSpawnPoint.position + direction * 100, Vector3.zero, false));
            }
        }

        if (enemyHit && hitmarkerImage != null)
        {
            ShowHitmarker();
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        canShoot = false;

        if (reloadSound != null && shotgunSound != null)
        {
            shotgunSound.PlayOneShot(reloadSound, 0.7f);
        }

        if (animator != null)
            animator.SetTrigger("Reload");

        yield return new WaitForSeconds(1.5f);

        BulletCount = MaxBullets;
        isReloading = false;
        canShoot = true;
        UpdateAmmoUI();
    }

    Vector3 GetDirection()
    {
        Vector3 direction = bulletSpawnPoint.forward;

        if (addBulletSpread)
        {
            float spreadMultiplier = isAiming ? 0.5f : 1f;
            direction += new Vector3(
                Random.Range(-bulletSpreadVariance.x, bulletSpreadVariance.x) * spreadMultiplier,
                Random.Range(-bulletSpreadVariance.y, bulletSpreadVariance.y) * spreadMultiplier,
                Random.Range(-bulletSpreadVariance.z, bulletSpreadVariance.z) * spreadMultiplier
            );
            direction.Normalize();
        }

        return direction;
    }

    IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool madeImpact)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(trail.transform.position, hitPoint);
        float remainingDistance = distance;

        while (remainingDistance > 0)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (remainingDistance / distance));
            remainingDistance -= bulletSpeed * Time.deltaTime;
            yield return null;
        }

        trail.transform.position = hitPoint;

        if (madeImpact && impactParticleSystem != null)
        {
            GameObject impact = Instantiate(impactParticleSystem.gameObject, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(impact, 2f);
        }

        Destroy(trail.gameObject, trail.time);
    }

    void ShowHitmarker()
    {
        if (hitmarkerImage != null)
        {
            hitmarkerImage.enabled = true;
            StartCoroutine(HideHitmarkerAfterDelay());
        }
    }

    IEnumerator HideHitmarkerAfterDelay()
    {
        yield return new WaitForSeconds(hitmarkerDisplayTime);
        if (hitmarkerImage != null)
            hitmarkerImage.enabled = false;
    }

    void UpdateAmmoUI()
    {
        if (BulletsText != null)
            BulletsText.text = $"{BulletCount} / {MaxBullets}";
    }

    public void SetCamera(Transform newCamera)
    {
        cameraTransform = newCamera;

        if (cameraTransform != null)
        {
            weaponCamera = cameraTransform.GetComponent<Camera>();
            SetInitialPosition();
        }
    }

    public void ResetToOriginalPosition()
    {
        if (cameraTransform != null)
        {
            SetInitialPosition();
        }
    }

    public void SetDamage(int newDamage)
    {
        damagePerPellet = Mathf.Max(1, newDamage);
    }

    public void IncreaseDamage(int damageIncrease)
    {
        damagePerPellet += damageIncrease;
    }

    public bool IsReloading()
    {
        return isReloading;
    }

    public bool CanShoot()
    {
        return canShoot && BulletCount > 0 && !isReloading;
    }

    public void SetAmmo(int newBulletCount, int newMaxBullets = -1)
    {
        BulletCount = Mathf.Clamp(newBulletCount, 0, MaxBullets);

        if (newMaxBullets > 0)
        {
            MaxBullets = newMaxBullets;
        }

        UpdateAmmoUI();
    }

    public void AddAmmo(int amount)
    {
        BulletCount = Mathf.Clamp(BulletCount + amount, 0, MaxBullets);
        UpdateAmmoUI();
    }

    public void SetAiming(bool aim)
    {
        if (canAim)
        {
            isAiming = aim;
            addBulletSpread = !aim;
            bulletSpreadVariance = aim ? new Vector3(0.05f, 0.05f, 0.05f) : new Vector3(0.1f, 0.1f, 0.1f);
            targetOffset = aim ? aimOffset : hipOffset;

            if (weaponCamera != null)
            {
                weaponCamera.fieldOfView = aim ? aimFOV : hipFOV;
            }
        }
    }

    public bool IsAiming()
    {
        return isAiming;
    }

    public void SetAutomaticFire(bool automatic)
    {
        automaticFire = automatic;
    }

    public void SetHipOffset(Vector3 offset)
    {
        hipOffset = offset;
        if (!isAiming)
        {
            targetOffset = offset;
        }
    }

    public void SetAimOffset(Vector3 offset)
    {
        aimOffset = offset;
        if (isAiming)
        {
            targetOffset = offset;
        }
    }
}