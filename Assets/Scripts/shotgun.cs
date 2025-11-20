using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class Shotgun : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource shotgunSound;

    [Header("Shooting Settings")]
    [SerializeField] private bool addBulletSpread = true;
    [SerializeField] private Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] private float shootDelay = 0.5f;
    [SerializeField] private LayerMask mask = -1;
    [SerializeField] private float bulletSpeed = 100;
    [SerializeField] private int pelletCount = 6;

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

    // Prywatne zmienne
    private Animator animator;
    private float lastShootTime;
    private bool canShoot;

    private void Start()
    {
        BulletCount = MaxBullets;
        canShoot = true;
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
        if (Input.GetKeyDown(KeyCode.R))
        {
            BulletCount = MaxBullets;
            UpdateAmmoUI();
        }

        if (BulletCount <= 0)
        {
            BulletCount = 0;
            canShoot = false;
        }
        else
        {
            canShoot = true;
        }

        UpdateAmmoUI();
    }

    public void Shoot()
    {
        if (lastShootTime + shootDelay < Time.time && canShoot)
        {
            BulletCount--;
            UpdateAmmoUI();

            // DWIÊK TYLKO RAZ NA STRZA£ - PRZED PÊTL¥
            if (shotgunSound != null)
                shotgunSound.Play();

            shootingSystem.Play();

            // Strzelba strzela wieloma pociskami, ale dŸwiêk gra tylko raz
            for (int i = 0; i < pelletCount; i++)
            {
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
                            enemyHealth.TakeDamage(1);
                        }
                    }
                }
                else
                {
                    TrailRenderer trail = Instantiate(bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                    StartCoroutine(SpawnTrail(trail, bulletSpawnPoint.position + direction * 100, Vector3.zero, false));
                }
            }

            lastShootTime = Time.time;
        }
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
}