
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Commander : MonoBehaviour
{
    [Header("視角（頭）")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.1f;
    public float maxHeadYaw = 100f;
    public float minPitch = -60f;
    public float maxPitch = 60f;

    [Header("輪椅")]
    public float forwardSpeed = 2.5f;
    public float backwardSpeed = 1.2f;
    public float turnSpeed = 90f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private float headYaw;
    private float pitch;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Start()
    {
        LockCursor(true);
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null && kb.escapeKey.wasPressedThisFrame) LockCursor(false);
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) LockCursor(true);

        if (mouse != null) Look(mouse.delta.ReadValue());

        moveInput = Vector2.zero;

        if (kb != null)
        {
            if (kb.wKey.isPressed) moveInput.y += 1f;
            if (kb.sKey.isPressed) moveInput.y -= 1f;
            if (kb.dKey.isPressed) moveInput.x += 1f;
            if (kb.aKey.isPressed) moveInput.x -= 1f;
        }
    }

    void FixedUpdate()
    {
        Drive(moveInput, Time.fixedDeltaTime);
    }

    public void Look(Vector2 delta)
    {
        // if (Cursor.lockState == CursorLockMode.Locked)
        // {
        //     Vector2 d = delta * mouseSensitivity;
        //     headYaw = Mathf.Clamp(headYaw + d.x, -maxHeadYaw, maxHeadYaw);
        //     pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
        // }
        Vector2 d = delta * mouseSensitivity;
        headYaw = Mathf.Clamp(headYaw + d.x, -maxHeadYaw, maxHeadYaw);
        pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);

        cameraPivot.localRotation = Quaternion.Euler(pitch, headYaw, 0f);
    }

    public void Drive(Vector2 move, float dt)
    {
        float throttle = move.y;
        float steer = move.x;

        Quaternion rotation = rb.rotation * Quaternion.Euler(0f, steer * turnSpeed * dt, 0f);
        rb.MoveRotation(rotation);

        float speed = throttle >= 0f ? forwardSpeed : backwardSpeed;
        Vector3 velocity = rotation * Vector3.forward * throttle * speed;

        rb.MovePosition(rb.position + velocity * dt);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
