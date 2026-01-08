using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class Rifle : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource gunSound;

    [Header("Shooting Settings")]
    [SerializeField] private bool addBulletSpread = true;
    [SerializeField] private Vector3 bulletSpreadVariance = new Vector3(0.05f, 0.05f, 0.05f);
    [SerializeField] private float shootDelay = 0.15f;
    [SerializeField] private LayerMask mask = -1;
    [SerializeField] private float bulletSpeed = 150;
    [SerializeField] private int damagePerBullet = 15;

    [Header("Aiming Settings")]
    [SerializeField] private bool canAim = true;
    [SerializeField] private Vector3 hipOffset = new Vector3(0.3f, -0.2f, 0.5f);
    [SerializeField] private Vector3 aimOffset = new Vector3(0f, -0.1f, 0.4f);
    [SerializeField] private float aimSpeed = 10f;
    [SerializeField] private float aimFOV = 40f;
    [SerializeField] private float hipFOV = 60f;
    private bool isAiming = false;
    private Camera weaponCamera;

    [Header("Camera Collision Prevention")]
    [SerializeField] private bool preventCameraClipping = true;
    [SerializeField] private float minDistanceFromCamera = 0.3f;
    [SerializeField] private float collisionSphereRadius = 0.25f;
    [SerializeField] private LayerMask cameraCollisionMask = -1;
    private Vector3 safeOffset;
    private bool isCameraClipping = false;

    [Header("Model Stabilization Settings - FIXED")]
    [SerializeField] private bool stabilizeModel = true;
    [SerializeField] private float positionSmoothTime = 0.05f;
    [SerializeField] private float rotationSmoothTime = 0.05f;
    [SerializeField] private float maxVelocityThreshold = 4f;
    [SerializeField] private float velocityMultiplier = 1.4f;
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
    public int MaxBullets = 30;
    public TextMeshProUGUI BulletsText;
    public GameObject BulletCounter;

    [Header("Camera & Player References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerTransform;

    [Header("Rifle Specific Settings")]
    public bool automaticFire = true;
    [SerializeField] private float reloadTime = 2.0f;
    [SerializeField] private AudioClip reloadSound;

    // PRIVATE VARIABLES
    private Animator animator;
    private float lastShootTime;
    private bool canShoot;
    private bool isReloading;
    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private Vector3 offsetVelocity = Vector3.zero;

    private void Start()
    {
        BulletCount = MaxBullets;
        canShoot = true;
        isReloading = false;
        currentOffset = hipOffset;
        targetOffset = hipOffset;
        safeOffset = hipOffset;
        UpdateAmmoUI();

        if (hitmarkerImage != null)
            hitmarkerImage.enabled = false;

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

        // Inicjalizacja bufora wyg�adzania
        if (useSmoothingFilter)
        {
            positionBuffer = new Vector3[smoothingBufferSize];
            for (int i = 0; i < smoothingBufferSize; i++)
            {
                positionBuffer[i] = transform.position;
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

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
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
            addBulletSpread = false;
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

    // RESZTA METOD BEZ ZMIAN (HandleInput, Shoot, Reload, itd.)...
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && BulletCount < MaxBullets)
        {
            StartCoroutine(Reload());
        }

        if (automaticFire)
        {
            if (Input.GetMouseButton(0) && canShoot && !isReloading)
            {
                Shoot();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && canShoot && !isReloading)
            {
                Shoot();
            }
        }

        if (BulletCount <= 0)
        {
            BulletCount = 0;
            canShoot = false;

            if (!isReloading)
            {
                StartCoroutine(Reload());
            }
        }
        else
        {
            canShoot = true;
        }
    }

    public void Shoot()
    {
        if (lastShootTime + shootDelay < Time.time && canShoot && !isReloading)
        {
            BulletCount--;
            UpdateAmmoUI();

            if (gunSound != null)
                gunSound.Play();

            shootingSystem.Play();
            Vector3 direction = GetDirection();

            if (Physics.Raycast(bulletSpawnPoint.position, direction, out RaycastHit hit, float.MaxValue, mask))
            {
                TrailRenderer trail = Instantiate(bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, true));

                if (hit.collider.CompareTag("Enemy"))
                {
                    ShowHitmarker();

                    EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damagePerBullet);
                    }
                }
            }
            else
            {
                TrailRenderer trail = Instantiate(bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, bulletSpawnPoint.position + direction * 100, Vector3.zero, false));
            }

            lastShootTime = Time.time;
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        canShoot = false;

        if (reloadSound != null && gunSound != null)
        {
            gunSound.PlayOneShot(reloadSound);
        }

        if (animator != null)
            animator.SetTrigger("Reload");

        yield return new WaitForSeconds(reloadTime);

        BulletCount = MaxBullets;
        isReloading = false;
        canShoot = true;
        UpdateAmmoUI();
    }

    private Vector3 GetDirection()
    {
        Vector3 direction = bulletSpawnPoint.forward;

        if (addBulletSpread && !isAiming)
        {
            direction += new Vector3(
                Random.Range(-bulletSpreadVariance.x, bulletSpreadVariance.x),
                Random.Range(-bulletSpreadVariance.y, bulletSpreadVariance.y),
                Random.Range(-bulletSpreadVariance.z, bulletSpreadVariance.z)
            );
            direction.Normalize();
        }

        return direction;
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool madeImpact)
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

        if (madeImpact)
        {
            Instantiate(impactParticleSystem, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        Destroy(trail.gameObject, trail.time);
    }

    private void ShowHitmarker()
    {
        if (hitmarkerImage != null)
        {
            hitmarkerImage.enabled = true;
            StartCoroutine(HideHitmarkerAfterDelay());
        }
    }

    private IEnumerator HideHitmarkerAfterDelay()
    {
        yield return new WaitForSeconds(hitmarkerDisplayTime);
        if (hitmarkerImage != null)
            hitmarkerImage.enabled = false;
    }

    private void UpdateAmmoUI()
    {
        if (BulletsText != null)
            BulletsText.text = BulletCount.ToString() + " / " + MaxBullets.ToString();
    }

    public void SetDamage(int newDamage) { damagePerBullet = newDamage; }
    public void IncreaseDamage(int damageIncrease) { damagePerBullet += damageIncrease; }
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
    public void SetAutomaticFire(bool automatic) { automaticFire = automatic; }
    public void SetReloadTime(float time) { reloadTime = time; }
    public bool IsReloading() { return isReloading; }
    public void SetAiming(bool aim)
    {
        if (canAim)
        {
            isAiming = aim;
            addBulletSpread = !aim;
            targetOffset = aim ? aimOffset : hipOffset;

            if (weaponCamera != null)
            {
                weaponCamera.fieldOfView = aim ? aimFOV : hipFOV;
            }
        }
    }
    public bool IsAiming() { return isAiming; }
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