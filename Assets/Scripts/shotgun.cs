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

    [Header("Aiming Spread Settings")]
    [SerializeField] private Vector3 aimSpreadVariance = new Vector3(0.05f, 0.05f, 0.05f);
    [SerializeField] private Vector3 hipSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);

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

    [Header("Camera Collision Prevention")]
    [SerializeField] private bool preventCameraClipping = true;
    [SerializeField] private float minDistanceFromCamera = 0.35f;
    [SerializeField] private float collisionSphereRadius = 0.3f;
    [SerializeField] private LayerMask cameraCollisionMask = -1;
    private Vector3 safeOffset;
    private bool isCameraClipping = false;

    [Header("Model Stabilization Settings - FIXED")]
    [SerializeField] private bool stabilizeModel = true;
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float rotationSmoothTime = 0.08f;
    [SerializeField] private float maxVelocityThreshold = 3f;
    [SerializeField] private float velocityMultiplier = 1.5f;
    [SerializeField] private bool useLateUpdateForStabilization = true;
    [SerializeField] private bool useSmoothingFilter = true;
    [SerializeField] private int smoothingBufferSize = 5;

    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;
    private Vector3 positionVelocity = Vector3.zero;
    private Quaternion smoothedRotation;
    private Vector3[] positionBuffer;
    private int bufferIndex = 0;

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

    [Header("Camera & Player References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerTransform;

    private Animator animator;
    private float lastShootTime;
    private bool canShoot = true;
    private bool isReloading = false;
    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private Vector3 offsetVelocity = Vector3.zero;

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
        UpdateAmmoUI();

        if (!useLateUpdateForStabilization)
        {
            UpdateWeaponPosition();
        }
    }

    private void LateUpdate()
    {
        if (useLateUpdateForStabilization)
        {
            UpdateWeaponPosition();
        }
    }

    private void InitializeShotgun()
    {
        BulletCount = MaxBullets;
        currentOffset = hipOffset;
        targetOffset = hipOffset;
        safeOffset = hipOffset;

        // Inicjalizacja bufora wyg³adzania
        if (useSmoothingFilter)
        {
            positionBuffer = new Vector3[smoothingBufferSize];
            for (int i = 0; i < smoothingBufferSize; i++)
            {
                positionBuffer[i] = transform.position;
            }
        }

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

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (cameraTransform != null)
        {
            lastCameraPosition = cameraTransform.position;
            lastCameraRotation = cameraTransform.rotation;

            SetInitialPosition();

            if (useSmoothingFilter)
            {
                for (int i = 0; i < smoothingBufferSize; i++)
                {
                    positionBuffer[i] = CalculateDesiredPosition();
                }
            }
        }
    }

    private void SetInitialPosition()
    {
        if (cameraTransform == null) return;

        Vector3 desiredPosition = cameraTransform.position +
                                 cameraTransform.right * hipOffset.x +
                                 cameraTransform.up * hipOffset.y +
                                 cameraTransform.forward * hipOffset.z;

        transform.position = desiredPosition;
        transform.rotation = cameraTransform.rotation;
        smoothedRotation = cameraTransform.rotation;
    }

    private void UpdateWeaponPosition()
    {
        if (cameraTransform == null) return;

        if (preventCameraClipping)
        {
            CheckCameraCollision();
        }

        currentOffset = Vector3.SmoothDamp(
            currentOffset,
            isCameraClipping ? safeOffset : targetOffset,
            ref offsetVelocity,
            0.1f,
            aimSpeed
        );

        if (stabilizeModel)
        {
            StabilizeWeaponPosition();
        }
        else
        {
            Vector3 desiredPosition = CalculateDesiredPosition();
            transform.position = desiredPosition;
            transform.rotation = cameraTransform.rotation;
        }
    }

    private Vector3 CalculateDesiredPosition()
    {
        if (cameraTransform == null) return transform.position;

        return cameraTransform.position +
               cameraTransform.right * currentOffset.x +
               cameraTransform.up * currentOffset.y +
               cameraTransform.forward * currentOffset.z;
    }

    private void StabilizeWeaponPosition()
    {
        if (cameraTransform == null) return;

        Vector3 cameraVelocity = (cameraTransform.position - lastCameraPosition) / Time.deltaTime;

        float velocityMultiplierValue = Mathf.Clamp01(cameraVelocity.magnitude / maxVelocityThreshold);
        velocityMultiplierValue = Mathf.Pow(velocityMultiplierValue, 1.5f);

        float currentPositionSmoothTime = positionSmoothTime * (1f + velocityMultiplierValue * velocityMultiplier);
        float currentRotationSmoothTime = rotationSmoothTime * (1f + velocityMultiplierValue * velocityMultiplier);

        Vector3 targetPosition = CalculateDesiredPosition();

        Vector3 smoothedTargetPosition;

        if (useSmoothingFilter)
        {
            positionBuffer[bufferIndex] = targetPosition;
            bufferIndex = (bufferIndex + 1) % smoothingBufferSize;

            Vector3 averagePosition = Vector3.zero;
            for (int i = 0; i < smoothingBufferSize; i++)
            {
                averagePosition += positionBuffer[i];
            }
            averagePosition /= smoothingBufferSize;

            smoothedTargetPosition = averagePosition;
        }
        else
        {
            smoothedTargetPosition = targetPosition;
        }

        Vector3 currentVelocity = positionVelocity;
        currentVelocity = Vector3.ClampMagnitude(currentVelocity, 10f);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            smoothedTargetPosition,
            ref currentVelocity,
            currentPositionSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        positionVelocity = currentVelocity;

        smoothedRotation = Quaternion.Slerp(
            smoothedRotation,
            cameraTransform.rotation,
            Time.deltaTime / currentRotationSmoothTime
        );

        transform.rotation = smoothedRotation;

        lastCameraPosition = cameraTransform.position;
        lastCameraRotation = cameraTransform.rotation;
    }

    private void CheckCameraCollision()
    {
        if (cameraTransform == null) return;

        Vector3 weaponPos = cameraTransform.position +
                           cameraTransform.right * targetOffset.x +
                           cameraTransform.up * targetOffset.y +
                           cameraTransform.forward * targetOffset.z;

        float distanceToCamera = Vector3.Distance(weaponPos, cameraTransform.position);
        Vector3 direction = (weaponPos - cameraTransform.position).normalized;
        float checkDistance = Mathf.Max(distanceToCamera, minDistanceFromCamera);

        RaycastHit[] hits = Physics.SphereCastAll(
            cameraTransform.position,
            collisionSphereRadius * 1.2f,
            direction,
            checkDistance + 0.1f,
            cameraCollisionMask,
            QueryTriggerInteraction.Ignore
        );

        isCameraClipping = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform == transform ||
                hit.collider.transform == cameraTransform ||
                (playerTransform != null && hit.collider.transform.IsChildOf(playerTransform)))
                continue;

            if (hit.distance < distanceToCamera)
            {
                isCameraClipping = true;

                float safetyMargin = 0.05f;
                safeOffset = new Vector3(
                    targetOffset.x,
                    targetOffset.y,
                    Mathf.Max(targetOffset.z, hit.distance + minDistanceFromCamera + safetyMargin)
                );
                break;
            }
        }

        if (distanceToCamera < minDistanceFromCamera * 0.8f && !isCameraClipping)
        {
            isCameraClipping = true;
            safeOffset = new Vector3(
                targetOffset.x,
                targetOffset.y,
                minDistanceFromCamera
            );
        }
    }

    private void HandleAiming()
    {
        if (!canAim || cameraTransform == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
            addBulletSpread = true; // ZMIENIONE: bullet spread dalej aktywny
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

    Vector3 GetDirection()
    {
        Vector3 direction = bulletSpawnPoint.forward;

        if (addBulletSpread)
        {
            // U¿yj odpowiedniego spreadu w zale¿noœci od celowania
            Vector3 currentSpread = isAiming ? aimSpreadVariance : hipSpreadVariance;

            direction += new Vector3(
                Random.Range(-currentSpread.x, currentSpread.x),
                Random.Range(-currentSpread.y, currentSpread.y),
                Random.Range(-currentSpread.z, currentSpread.z)
            );
            direction.Normalize();
        }

        return direction;
    }

    // RESZTA METOD BEZ ZMIAN...
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
            lastCameraPosition = cameraTransform.position;
            lastCameraRotation = cameraTransform.rotation;

            if (stabilizeModel && useSmoothingFilter)
            {
                for (int i = 0; i < smoothingBufferSize; i++)
                {
                    positionBuffer[i] = CalculateDesiredPosition();
                }
            }
        }
    }

    public void SetPlayer(Transform player) { playerTransform = player; }
    public void ResetToOriginalPosition() { if (cameraTransform != null) SetInitialPosition(); }
    public void SetDamage(int newDamage) { damagePerPellet = Mathf.Max(1, newDamage); }
    public void IncreaseDamage(int damageIncrease) { damagePerPellet += damageIncrease; }
    public bool IsReloading() { return isReloading; }
    public bool CanShoot() { return canShoot && BulletCount > 0 && !isReloading; }
    public void SetAmmo(int newBulletCount, int newMaxBullets = -1)
    {
        BulletCount = Mathf.Clamp(newBulletCount, 0, MaxBullets);
        if (newMaxBullets > 0) MaxBullets = newMaxBullets;
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
            addBulletSpread = true; // ZMIENIONE: zawsze aktywny spread
            targetOffset = aim ? aimOffset : hipOffset;

            if (weaponCamera != null)
            {
                weaponCamera.fieldOfView = aim ? aimFOV : hipFOV;
            }
        }
    }
    public bool IsAiming() { return isAiming; }
    public void SetAutomaticFire(bool automatic) { automaticFire = automatic; }
    public void SetHipOffset(Vector3 offset) { hipOffset = offset; if (!isAiming) targetOffset = offset; }
    public void SetAimOffset(Vector3 offset) { aimOffset = offset; if (isAiming) targetOffset = offset; }
    public void SetPreventCameraClipping(bool prevent) { preventCameraClipping = prevent; }
    public void SetMinDistanceFromCamera(float distance) { minDistanceFromCamera = Mathf.Max(0.1f, distance); }
    public void SetStabilizeModel(bool stabilize) { stabilizeModel = stabilize; }
    public void SetStabilizationSettings(float posSmoothTime, float rotSmoothTime, float maxVelocity)
    {
        positionSmoothTime = posSmoothTime;
        rotationSmoothTime = rotSmoothTime;
        maxVelocityThreshold = maxVelocity;
    }
    public void SetSmoothingFilter(bool useFilter, int bufferSize = 5)
    {
        useSmoothingFilter = useFilter;
        smoothingBufferSize = bufferSize;

        if (useSmoothingFilter && cameraTransform != null)
        {
            positionBuffer = new Vector3[smoothingBufferSize];
            for (int i = 0; i < smoothingBufferSize; i++)
            {
                positionBuffer[i] = CalculateDesiredPosition();
            }
        }
    }
}