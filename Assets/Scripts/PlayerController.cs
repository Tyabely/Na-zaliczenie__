using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    
    public float CurrentStamina;
    
    public GameObject Stats;

    [Header("Combat")]
    private PlayerAction playerAction;

    [Header("References")]
    public Rigidbody rb;
    public Transform head;
    public new Camera camera;

    [Header("Config")]
    public float WalkSpeed = 5f;
    public float RunSpeed = 10f;
    public float jumpSpeed = 7f;
    public float itemPickupDistance;

    [Header("Camera Effects")]
    public float baseFov = 60f;
    public float baseCameraHeight = .85f;
    public float walkBobbingRate = .75f;
    public float runBobbingRate = 1f;
    public float maxWalkBobbingOffset = .2f;
    public float maxRunBobbingOffset = .3f;
    public float cameraShakeThreshold = 10f;
    [Range(0f, 0.03f)] public float cameraShakeRate = 0.015f;
    public float maxVerticalFallShakeAngle = 40f;
    public float maxHorizontalFallShakeAngle = 40f;

    [Header("Audio")]
    public AudioSource audioWalk;
    public AudioSource audioWind;
    public AudioSource audioWalkPavement;
    public AudioSource audioWalkWood;
    public float windPitchMultiplier;
    public float walkPitch = 1f;
    public float runPitch = 1.5f;

    [Header("Surface Detection")]
    public float surfaceChangeLogCooldown = 0.5f;

    [Header("Object Pickup")]
    public Vector3 pickupOffset = new Vector3(0, 0, 1.5f);

    // Runtime variables
    private Vector3 newVelocity;
    private bool isGrounded = false;
    private bool isJumping = false;
    public bool isMoving = false;
    public bool isRunning = false;
    private float vyCache;
    private string activeAudioName = "default";
    private string lastSurfaceName = "";
    private float lastSurfaceChangeTime;
    

    // Object pickup variables
    private Transform attachedObject = null;
    private Rigidbody attachedRigidbody = null;
    private Collider attachedCollider = null;
    private Collider playerCollider;
    private bool wasKinematic;


    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        playerCollider = GetComponent<Collider>();

   
        InitializePlayerAction();

        // Konfiguracja audio
        ConfigureAudioSources();
    }

    void InitializePlayerAction()
    {
        // Szukaj PlayerAction na tym samym obiekcie
        playerAction = GetComponent<PlayerAction>();

        // Jeśli nie ma na tym samym obiekcie, szukaj w dzieciach
        if (playerAction == null)
            playerAction = GetComponentInChildren<PlayerAction>();

        // Jeśli nadal nie ma, szukaj w całej scenie (NOWA METODA)
        if (playerAction == null)
            playerAction = FindFirstObjectByType<PlayerAction>();

        if (playerAction == null)
            Debug.LogWarning("PlayerAction not found! Shooting will not work.");
        else
            Debug.Log("PlayerAction reference found successfully!");
    }

    void ConfigureAudioSources()
    {
        // Ustawienie podstawowych w³aœciwoœci audio sources
        if (audioWalk != null)
        {
            audioWalk.loop = true;
            audioWalk.playOnAwake = false;
            audioWalk.Stop(); // Upewnij się że nie gra na start
        }
        if (audioWalkPavement != null)
        {
            audioWalkPavement.loop = true;
            audioWalkPavement.playOnAwake = false;
            audioWalkPavement.Stop();
        }
        if (audioWalkWood != null)
        {
            audioWalkWood.loop = true;
            audioWalkWood.playOnAwake = false;
            audioWalkWood.Stop();
        }
        if (audioWind != null)
        {
            audioWind.loop = true;
            audioWind.playOnAwake = false;
        }
    }

    void Update()
    {


        CurrentStamina = Stats.GetComponent<Stats>().CurrentStamina;

        HandleRotation();
        HandleMovement();
        HandleHeadBobbing();
        HandleAudio();
        HandleObjectPickup();
        HandleShooting();
    }

    void HandleShooting()
    {
        // Strzelanie na lewy przycisk myszy (LPM)
        if (Input.GetMouseButtonDown(0))
        {
            if (playerAction != null)
            {
                playerAction.OnShoot();
            }
        }
    }

    void FixedUpdate()
    {
        CheckGroundSurface();
    }

    void LateUpdate()
    {
        HandleCameraRotation();
        HandleFOVEffects();
        HandleCameraShake();
    }

    void HandleRotation()
    {
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * 2f);
    }

    void HandleMovement()
    {
        newVelocity = Vector3.up * rb.linearVelocity.y;

        // Sprawdzanie czy gracz mo¿e biegaæ (ma staminê)
        bool canRun = CurrentStamina > 0 && Input.GetKey(KeyCode.LeftShift); 
        float speed = canRun ? RunSpeed : WalkSpeed;

        newVelocity.x = Input.GetAxis("Horizontal") * speed;
        newVelocity.z = Input.GetAxis("Vertical") * speed;

        // Sprawdzanie czy gracz siê porusza
        isMoving = (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f) && isGrounded;

        // Sprawdzanie czy gracz biegnie
        isRunning = canRun && isMoving;

        if (isGrounded && Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            newVelocity.y = jumpSpeed;
            isJumping = true;
        }

        rb.linearVelocity = transform.TransformDirection(newVelocity);
    }

    

    void HandleHeadBobbing()
    {
        bool isMovingOnGround = (Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f) && isGrounded;

        if (isMovingOnGround)
        {
            float bobbingRate = Input.GetKey(KeyCode.LeftShift) ? runBobbingRate : walkBobbingRate;
            float bobbingOffset = Input.GetKey(KeyCode.LeftShift) ? maxRunBobbingOffset : maxWalkBobbingOffset;
            Vector3 targetHeadPosition = Vector3.up * baseCameraHeight +
                Vector3.up * (Mathf.PingPong(Time.time * bobbingRate, bobbingOffset)) -
                (Vector3.up * bobbingOffset * 0.5f);
            head.localPosition = Vector3.Lerp(head.localPosition, targetHeadPosition, .1f);
        }
    }

    void HandleAudio()
    {
        bool isMovingOnGround = (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f) && isGrounded;
        float currentPitch = (Input.GetKey(KeyCode.LeftShift) && CurrentStamina > 0) ? runPitch : walkPitch;

        // Lista wszystkich audio sources
        AudioSource[] allWalkSounds = { audioWalk, audioWalkPavement, audioWalkWood };

        if (isMovingOnGround)
        {
            // Tylko ustaw pitch - zmiana dźwięku jest już obsługiwana w CheckGroundSurface()
            foreach (AudioSource sound in allWalkSounds)
            {
                if (sound != null)
                {
                    //sound.pitch = currentPitch;

                    // Upewnij się że odpowiedni dźwięk gra
                    bool shouldPlay = (sound == audioWalk && activeAudioName == "default") ||
                                     (sound == audioWalkPavement && activeAudioName == "Pavement") ||
                                     (sound == audioWalkWood && activeAudioName == "Wood");

                    if (shouldPlay && !sound.isPlaying)
                    {
                        sound.Play();
                    }
                    else if (!shouldPlay && sound.isPlaying)
                    {
                        sound.Stop();
                    }
                }
            }
        }
        else
        {
            // Wyłącz wszystkie dźwięki chodzenia
            foreach (AudioSource sound in allWalkSounds)
            {
                if (sound != null && sound.isPlaying)
                {
                    sound.Stop();
                }
            }
        }

        // Wind audio
        if (audioWind != null)
        {
            if (!audioWind.isPlaying)
                audioWind.Play();

            float speed = rb.linearVelocity.magnitude;
            audioWind.pitch = Mathf.Clamp(speed * 0.1f, 0.5f, 1.5f);
            audioWind.volume = Mathf.Clamp(speed * 0.03f, 0.05f, 0.2f);
        }

        // DEBUG: Pokazuj który dźwięk aktualnie gra
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log($"=== AUDIO DEBUG ===");
            Debug.Log($"Active surface: {activeAudioName}");
            Debug.Log($"isMovingOnGround: {isMovingOnGround}");
            Debug.Log($"Walk sound: {audioWalk != null && audioWalk.isPlaying}");
            Debug.Log($"Pavement sound: {audioWalkPavement != null && audioWalkPavement.isPlaying}");
            Debug.Log($"Wood sound: {audioWalkWood != null && audioWalkWood.isPlaying}");
            Debug.Log($"===================");
        }
    }

    void HandleObjectPickup()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (attachedObject != null)
            {
                // Drop object
                attachedObject.SetParent(null);

                if (attachedRigidbody != null)
                {
                    attachedRigidbody.isKinematic = wasKinematic;
                }
                if (attachedCollider != null)
                {
                    attachedCollider.enabled = true;
                    Physics.IgnoreCollision(playerCollider, attachedCollider, false);
                }

                attachedObject = null;
                attachedRigidbody = null;
                attachedCollider = null;
            }
            else
            {
                // Try to pick up object
                if (Physics.Raycast(head.position, head.forward, out RaycastHit hit, itemPickupDistance) &&
                    hit.transform.CompareTag("Pickable"))
                {
                    attachedObject = hit.transform;
                    attachedObject.SetParent(head);
                    attachedObject.localPosition = pickupOffset;
                    attachedObject.localRotation = Quaternion.identity;

                    if (attachedObject.TryGetComponent<Rigidbody>(out attachedRigidbody))
                    {
                        wasKinematic = attachedRigidbody.isKinematic;
                        attachedRigidbody.isKinematic = true;
                    }
                    if (attachedObject.TryGetComponent<Collider>(out attachedCollider))
                    {
                        attachedCollider.enabled = false;
                        Physics.IgnoreCollision(playerCollider, attachedCollider, true);
                    }
                }
            }
        }
    }

    void UpdateWalkSoundImmediately()
    {
        bool isMovingOnGround = (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f) && isGrounded;
        float currentPitch = (Input.GetKey(KeyCode.LeftShift) && CurrentStamina > 0) ? runPitch : walkPitch;

        // Lista wszystkich audio sources
        AudioSource[] allWalkSounds = { audioWalk, audioWalkPavement, audioWalkWood };

        if (isMovingOnGround)
        {
            // Ustaw pitch dla wszystkich
            foreach (AudioSource sound in allWalkSounds)
            {
                if (sound != null) sound.pitch = currentPitch; 
            }

            // Znajdź i włącz odpowiedni dźwięk
            AudioSource soundToPlay = null;

            if (activeAudioName == "default" && audioWalk != null)
                soundToPlay = audioWalk;
            else if (activeAudioName == "Pavement" && audioWalkPavement != null)
                soundToPlay = audioWalkPavement;
            else if (activeAudioName == "Wood" && audioWalkWood != null)
                soundToPlay = audioWalkWood;
            else if (audioWalk != null) // Fallback na domyślny
                soundToPlay = audioWalk;

            // Włącz odpowiedni dźwięk, wyłącz pozostałe
            foreach (AudioSource sound in allWalkSounds)
            {
                if (sound != null)
                {
                    if (sound == soundToPlay)
                    {
                        if (!sound.isPlaying)
                        {
                            sound.Play();
                            Debug.Log($"Immediately playing {activeAudioName} walk sound");
                        }
                    }
                    else
                    {
                        if (sound.isPlaying)
                        {
                            sound.Stop();
                            Debug.Log($"Immediately stopping {sound.name}");
                        }
                    }
                }
            }
        }
        else
        {
            // Wyłącz wszystkie dźwięki chodzenia
            foreach (AudioSource sound in allWalkSounds)
            {
                if (sound != null && sound.isPlaying)
                {
                    sound.Stop();
                    Debug.Log($"Stopping {sound.name} - not moving");
                }
            }
        }
    }

    void CheckGroundSurface()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.1f);

        if (isGrounded && hit.collider != null)
        {
            // WYKRYWANIE POWIERZCHNI PO TAGU
            string newSurfaceName = "default"; // domyślny

            if (hit.collider.CompareTag("Pavement"))
            {
                newSurfaceName = "Pavement";
            }
            else if (hit.collider.CompareTag("Wood"))
            {
                newSurfaceName = "Wood";
            }

            // JEŚLI POWIERZCHNIA SIĘ ZMIENIŁA - NATYCHMIAST ZAKTUALIZUJ DŹWIĘK
            if (newSurfaceName != activeAudioName)
            {
                Debug.Log($"Surface changed from {activeAudioName} to {newSurfaceName}");
                activeAudioName = newSurfaceName;

                // NATYCHMIAST ZAKTUALIZUJ DŹWIĘK - nawet podczas ruchu
                UpdateWalkSoundImmediately();
            }

            if (newSurfaceName != lastSurfaceName && Time.time - lastSurfaceChangeTime >= surfaceChangeLogCooldown)
            {
                Debug.Log("Surface changed to: " + newSurfaceName + " | Tag: " + hit.collider.tag + " | Object: " + hit.collider.gameObject.name);
                lastSurfaceName = newSurfaceName;
                lastSurfaceChangeTime = Time.time;
            }
        }
        else
        {
            if (activeAudioName != "default")
            {
                activeAudioName = "default";
                UpdateWalkSoundImmediately(); // Aktualizuj też gdy tracisz kontakt z ziemią
            }
        }

        vyCache = rb.linearVelocity.y;
    }

    void HandleCameraRotation()
    {
        Vector3 e = head.eulerAngles;
        e.x -= Input.GetAxis("Mouse Y") * 2f;
        e.x = RestrictAngle(e.x, -85f, 85f);
        head.eulerAngles = e;
    }

    void HandleFOVEffects()
    {
        float fovOffset = (rb.linearVelocity.y < 0f) ? Mathf.Sqrt(Mathf.Abs(rb.linearVelocity.y)) : 0f;
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, baseFov + fovOffset, .25f);
    }

    void HandleCameraShake()
    {
        if (isGrounded && Mathf.Abs(vyCache) >= cameraShakeThreshold)
        {
            float shakeIntensity = Mathf.Clamp01(Mathf.Abs(vyCache) / cameraShakeThreshold);

            Vector3 newAngle = head.localEulerAngles;
            newAngle += Vector3.right * UnityEngine.Random.Range(
                -maxVerticalFallShakeAngle * shakeIntensity,
                maxVerticalFallShakeAngle * shakeIntensity);
            newAngle += Vector3.up * UnityEngine.Random.Range(
                -maxHorizontalFallShakeAngle * shakeIntensity,
                maxHorizontalFallShakeAngle * shakeIntensity);

            head.localEulerAngles = Vector3.Lerp(
                head.localEulerAngles,
                newAngle,
                cameraShakeRate * shakeIntensity);
        }
        else
        {
            Vector3 e = head.localEulerAngles;
            e.y = 0f;
            head.localEulerAngles = e;
        }
    }


    void OnCollisionEnter(Collision col)
    {
        if (Vector3.Dot(col.GetContact(0).normal, Vector3.up) > 0.5f)
        {
            isGrounded = true;
            isJumping = false;
        }
        else if (Vector3.Dot(col.GetContact(0).normal, Vector3.up) < 0.5f)
        {
            if (rb.linearVelocity.y < -5f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            }
        }
    }

    void OnCollisionStay(Collision col)
    {
        if (Vector3.Dot(col.GetContact(0).normal, Vector3.up) > 0.5f)
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision col)
    {
        isGrounded = false;
    }

    public static float RestrictAngle(float angle, float angleMin, float angleMax)
    {
        if (angle > 180) angle -= 360;
        else if (angle < -180) angle += 360;
        return Mathf.Clamp(angle, angleMin, angleMax);
    }
}