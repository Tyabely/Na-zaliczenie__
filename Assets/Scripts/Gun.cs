using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class Gun : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource gunSound;

    [Header("Shooting Settings")]
    [SerializeField] private bool addBulletSpread = true;
    [SerializeField] private Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] private float shootDelay = 0.5f;
    [SerializeField] private LayerMask mask = -1;
    [SerializeField] private float bulletSpeed = 100;
    [SerializeField] private int damagePerBullet = 1;

    [Header("Aiming Settings")]
    [SerializeField] private bool canAim = true;
    [SerializeField] private Vector3 hipOffset = new Vector3(0.35f, -0.15f, 0.6f);
    [SerializeField] private Vector3 aimOffset = new Vector3(0f, -0.05f, 0.5f);
    [SerializeField] private float aimSpeed = 8f;
    [SerializeField] private float aimFOV = 45f;
    [SerializeField] private float hipFOV = 60f;
    private bool isAiming = false;
    private Camera weaponCamera;

    [Header("Camera Collision Prevention")]
    [SerializeField] private bool preventCameraClipping = true;
    [SerializeField] private float minDistanceFromCamera = 0.3f;
    [SerializeField] private float collisionSphereRadius = 0.2f;
    [SerializeField] private LayerMask cameraCollisionMask = -1;
    private Vector3 safeOffset;
    private bool isCameraClipping = false;

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
    public int MaxBullets = 12;
    public TextMeshProUGUI BulletsText;
    public GameObject BulletCounter;

    [Header("Camera & Player References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerTransform;

    [Header("Model Rotation Fix")]
    [SerializeField] private Vector3 modelRotationOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private bool fixModelRotation = true;

    // PUBLIC FIELDS
    public bool automaticFire = false;
    public bool canShoot = true;
    public bool isReloading = false;

    // PRIVATE VARIABLES
    private Animator animator;
    private float lastShootTime;
    private Vector3 currentOffset;
    private Vector3 targetOffset;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        BulletCount = MaxBullets;
        currentOffset = hipOffset;
        targetOffset = hipOffset;
        safeOffset = hipOffset;
        UpdateAmmoUI();

        if (hitmarkerImage != null)
            hitmarkerImage.enabled = false;

        ApplyModelRotationFix();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }

        weaponCamera = cameraTransform?.GetComponent<Camera>();

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
            SetInitialPosition();
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleAiming();
        HandleShooting();
        HandleReload();
        UpdateAmmoUI();

        UpdateWeaponPosition();
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

        // SprawdŸ kolizjê z kamer¹ przed aktualizacj¹ pozycji
        if (preventCameraClipping)
        {
            CheckCameraCollision();
        }

        currentOffset = Vector3.SmoothDamp(
            currentOffset,
            isCameraClipping ? safeOffset : targetOffset,
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

        if (fixModelRotation)
        {
            ApplyModelRotationFix();
        }
    }

    private void CheckCameraCollision()
    {
        if (cameraTransform == null) return;

        // Oblicz aktualn¹ pozycjê broni
        Vector3 weaponPos = cameraTransform.position +
                           cameraTransform.right * targetOffset.x +
                           cameraTransform.up * targetOffset.y +
                           cameraTransform.forward * targetOffset.z;

        // SprawdŸ odleg³oœæ od kamery
        float distanceToCamera = Vector3.Distance(weaponPos, cameraTransform.position);

        // SprawdŸ kolizjê sphere cast
        Vector3 direction = (weaponPos - cameraTransform.position).normalized;
        float checkDistance = Mathf.Max(distanceToCamera, minDistanceFromCamera);

        RaycastHit[] hits = Physics.SphereCastAll(
            cameraTransform.position,
            collisionSphereRadius,
            direction,
            checkDistance,
            cameraCollisionMask
        );

        isCameraClipping = false;

        foreach (RaycastHit hit in hits)
        {
            // Ignoruj w³asny collider i collidery gracza
            if (hit.collider.transform == transform ||
                hit.collider.transform == playerTransform ||
                (playerTransform != null && hit.collider.transform.IsChildOf(playerTransform)))
                continue;

            // Jeœli trafiliœmy obiekt miêdzy broni¹ a kamer¹
            if (hit.distance < distanceToCamera)
            {
                isCameraClipping = true;

                // Oblicz bezpieczny offset (odsuniêty od kamery)
                float penetrationDepth = distanceToCamera - hit.distance + minDistanceFromCamera;
                safeOffset = new Vector3(
                    targetOffset.x,
                    targetOffset.y,
                    Mathf.Max(targetOffset.z, hit.distance + minDistanceFromCamera)
                );

                Debug.Log($"Gun: Camera clipping detected! Adjusting Z from {targetOffset.z:F2} to {safeOffset.z:F2}");
                break;
            }
        }

        // Dodatkowe sprawdzenie minimalnej odleg³oœci
        if (distanceToCamera < minDistanceFromCamera && !isCameraClipping)
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

    private void HandleShooting()
    {
        if (isReloading || !canShoot || BulletCount <= 0) return;

        if (automaticFire)
        {
            if (Input.GetMouseButton(0))
            {
                if (Time.time - lastShootTime > shootDelay)
                {
                    Shoot();
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
        }
    }

    private void HandleReload()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && BulletCount < MaxBullets)
        {
            StartCoroutine(Reload());
        }

        if (BulletCount <= 0 && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        canShoot = false;

        yield return new WaitForSeconds(1.5f);

        BulletCount = MaxBullets;
        isReloading = false;
        canShoot = true;
        UpdateAmmoUI();
    }

    public void Shoot()
    {
        if (lastShootTime + shootDelay < Time.time && canShoot && !isReloading)
        {
            BulletCount--;
            UpdateAmmoUI();

            if (gunSound != null)
                gunSound.Play();

            if (shootingSystem != null)
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
            BulletsText.text = $"{BulletCount} / {MaxBullets}";
    }

    private void ApplyModelRotationFix()
    {
        transform.localRotation *= Quaternion.Euler(modelRotationOffset);
    }

    // PUBLIC METHODS
    public void SetDamage(int newDamage)
    {
        damagePerBullet = newDamage;
    }

    public void IncreaseDamage(int damageIncrease)
    {
        damagePerBullet += damageIncrease;
    }

    public void SetCamera(Transform newCamera)
    {
        cameraTransform = newCamera;
        weaponCamera = newCamera?.GetComponent<Camera>();

        if (cameraTransform != null)
        {
            SetInitialPosition();
        }
    }

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    public void ResetToOriginalPosition()
    {
        if (cameraTransform != null)
        {
            SetInitialPosition();
        }
    }

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

    public bool IsAiming()
    {
        return isAiming;
    }

    public bool IsReloading()
    {
        return isReloading;
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

    // DODANE: Metody do zapobiegania kolizji
    public void SetPreventCameraClipping(bool prevent)
    {
        preventCameraClipping = prevent;
    }

    public void SetMinDistanceFromCamera(float distance)
    {
        minDistanceFromCamera = Mathf.Max(0.1f, distance);
    }

    void OnDrawGizmosSelected()
    {
        if (cameraTransform != null && preventCameraClipping)
        {
            // Narysuj sphere collision check
            Gizmos.color = isCameraClipping ? Color.red : Color.yellow;
            Vector3 weaponPos = cameraTransform.position +
                               cameraTransform.right * targetOffset.x +
                               cameraTransform.up * targetOffset.y +
                               cameraTransform.forward * targetOffset.z;

            Gizmos.DrawWireSphere(weaponPos, collisionSphereRadius);

            // Linia do kamery
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(cameraTransform.position, weaponPos);
        }
    }
}