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

    [Header("Camera Follow")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 originalLocalPosition;
    [SerializeField] private bool useOriginalPosition = true;

    [Header("Rifle Specific Settings")]
    [SerializeField] private bool automaticFire = true;
    [SerializeField] private float reloadTime = 2.0f;
    [SerializeField] private AudioClip reloadSound;

    // Prywatne zmienne
    private Animator animator;
    private float lastShootTime;
    private bool canShoot;
    private bool isReloading;

    private void Start()
    {
        BulletCount = MaxBullets;
        canShoot = true;
        isReloading = false;
        UpdateAmmoUI();

        if (hitmarkerImage != null)
            hitmarkerImage.enabled = false;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleInput();
        UpdateAmmoUI();
        FollowCamera();
    }

    private void HandleInput()
    {
        // Prze³adowanie
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && BulletCount < MaxBullets)
        {
            StartCoroutine(Reload());
        }

        // Strzelanie
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

        // Sprawdzenie amunicji
        if (BulletCount <= 0)
        {
            BulletCount = 0;
            canShoot = false;

            // Automatyczne prze³adowanie gdy skoñczy siê amunicja
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

    private void FollowCamera()
    {
        if (cameraTransform != null)
        {
            transform.position = cameraTransform.position;
            transform.rotation = cameraTransform.rotation;

            if (useOriginalPosition)
            {
                transform.localPosition = originalLocalPosition;
            }
        }
    }

    public void Shoot()
    {
        if (lastShootTime + shootDelay < Time.time && canShoot && !isReloading)
        {
            BulletCount--;
            UpdateAmmoUI();

            // Odtwórz dŸwiêk strza³u
            if (gunSound != null)
                gunSound.Play();

            // Animacja strza³u
            //if (animator != null)
              //  animator.SetTrigger("Shoot");

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

        // Odtwórz dŸwiêk prze³adowania
        if (reloadSound != null && gunSound != null)
        {
            gunSound.PlayOneShot(reloadSound);
        }

        // Animacja prze³adowania
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

        if (addBulletSpread)
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
    }

    public void ResetToOriginalPosition()
    {
        if (cameraTransform != null)
        {
            transform.position = cameraTransform.position;
            transform.rotation = cameraTransform.rotation;
            transform.localPosition = originalLocalPosition;
        }
    }

    // Metody specyficzne dla karabinu
    public void SetAutomaticFire(bool automatic)
    {
        automaticFire = automatic;
    }

    public void SetReloadTime(float time)
    {
        reloadTime = time;
    }

    public bool IsReloading()
    {
        return isReloading;
    }
}