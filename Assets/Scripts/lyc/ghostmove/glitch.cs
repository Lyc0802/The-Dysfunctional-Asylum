using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class glitch : MonoBehaviour
{
    [Header("視角（頭）")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.1f;
    public float maxHeadYaw = 100f;
    public float minPitch = -60f;
    public float maxPitch = 60f;
    public float moveSpeed = 3f;
    public float bobAmplitude = 0.05f;      // 上下晃動幅度
    public float bobFrequency = 0.8f;       // 上下晃動頻率
    private Rigidbody rb;
    private Vector2 moveInput;
    private float headYaw;
    
    private float pitch;
    private Vector3 pivotBasePos;
    void Awake()
    {
        pivotBasePos = cameraPivot.localPosition;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }
    void Start()
    {
        LockCursor(true);
    }
    void FixedUpdate()
    {
        Move(moveInput, Time.fixedDeltaTime);
    }
    
    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        Hover(); 
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
    void LateUpdate()
    {
    cameraPivot.rotation = Quaternion.Euler(pitch, headYaw, 0f);
    }
    public void Move(Vector2 move,float dt)
    {
        float forward = move.y;   
        float strafe = move.x;    

        Quaternion rotation = Quaternion.Euler(0f, headYaw, 0f);
        rb.MoveRotation(rotation);

        Vector3 direction = rotation * new Vector3(strafe, 0f, forward);
        Vector3 velocity = Vector3.ClampMagnitude(direction, 1f) * moveSpeed;

    rb.MovePosition(rb.position + velocity * dt);
    }

    void Hover()   
    {
        float bobOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        cameraPivot.localPosition = pivotBasePos + Vector3.up * bobOffset;
    }
    public void Look(Vector2 delta)
    {
        Vector2 d = delta * mouseSensitivity;
        
        headYaw += d.x;   
        pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);

        //cameraPivot.localRotation = Quaternion.Euler(pitch, headYaw, 0f);
    }
    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
