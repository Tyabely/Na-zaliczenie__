// W Rifle.cs DODAJ te zmienne:
[Header("Camera Collision Prevention")]
[SerializeField] private bool preventCameraClipping = true;
[SerializeField] private float minDistanceFromCamera = 0.3f;
[SerializeField] private float collisionSphereRadius = 0.25f;
[SerializeField] private LayerMask cameraCollisionMask = -1;
private Vector3 safeOffset;
private bool isCameraClipping = false;

// W UpdateWeaponPosition() DODAJ:
private void UpdateWeaponPosition()
{
    if (cameraTransform == null) return;

    // SprawdŸ kolizjê z kamer¹
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
}

// DODAJ tê metodê:
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
        collisionSphereRadius,
        direction,
        checkDistance,
        cameraCollisionMask
    );

    isCameraClipping = false;

    foreach (RaycastHit hit in hits)
    {
        if (hit.collider.transform == transform ||
            (playerTransform != null && hit.collider.transform.IsChildOf(playerTransform)))
            continue;

        if (hit.distance < distanceToCamera)
        {
            isCameraClipping = true;
            float penetrationDepth = distanceToCamera - hit.distance + minDistanceFromCamera;
            safeOffset = new Vector3(
                targetOffset.x,
                targetOffset.y,
                Mathf.Max(targetOffset.z, hit.distance + minDistanceFromCamera)
            );
            break;
        }
    }

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

// W SetInitialPosition() upewnij siê ¿e jest:
private void SetInitialPosition()
{
    safeOffset = hipOffset; // DODAJ tê liniê
    // ... reszta kodu
}