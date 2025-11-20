using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Transform head;
    public Camera Component;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;

    private Vector3 movement;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMovementInput();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void HandleMovementInput()
    {

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = head.forward;
        Vector3 right = head.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();


        movement = (forward * vertical + right * horizontal).normalized;

    }

    void ApplyMovement()
    {

        Vector3 velocity = rb.linearVelocity;
        velocity.x = movement.x;
        velocity.z = movement.z;

        rb.linearVelocity = velocity;
    }
}//Igor Szawio³a