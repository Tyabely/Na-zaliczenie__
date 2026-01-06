using UnityEngine;

public class CameraColliderHelper : MonoBehaviour
{
    [Header("Collider Settings")]
    [SerializeField] private float colliderRadius = 0.2f;
    [SerializeField] private Vector3 colliderOffset = Vector3.zero;
    [SerializeField] private LayerMask collisionMask = -1;

    private SphereCollider sphereCollider;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Dodaj Sphere Collider do kamery
        sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = colliderRadius;
        sphereCollider.center = colliderOffset;
        sphereCollider.isTrigger = true;

        Debug.Log($"Camera collider added to {gameObject.name} with radius: {colliderRadius}");
    }

    void OnTriggerStay(Collider other)
    {
        // Ignoruj samego siebie
        if (other.transform == transform) return;

        // Ignoruj bronie i gracza
        if (other.GetComponent<Gun>() != null ||
            other.GetComponent<Shotgun>() != null ||
            other.GetComponent<Rifle>() != null ||
            other.CompareTag("Player"))
            return;

        // Debugowanie kolizji
        Debug.Log($"Camera colliding with: {other.name} ({other.tag})");
    }

    void OnDrawGizmosSelected()
    {
        if (cam != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + colliderOffset, colliderRadius);
        }
    }

    public float GetColliderRadius() { return colliderRadius; }
    public void SetColliderRadius(float radius)
    {
        colliderRadius = Mathf.Max(0.1f, radius);
        if (sphereCollider != null) sphereCollider.radius = colliderRadius;
    }
}