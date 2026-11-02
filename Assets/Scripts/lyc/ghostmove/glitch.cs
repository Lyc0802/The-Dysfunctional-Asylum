using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class glitch : MonoBehaviour
{
    [Header("視角（頭）")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.1f;
    public float minPitch = -60f;
    public float maxPitch = 60f;

    [Header("移動")]
    public float moveSpeed = 3f;

    [Header("漂浮晃動")]
    public float bobAmplitude = 0.05f;      // 上下晃動幅度
    public float bobFrequency = 0.8f;       // 每秒晃幾次

    private CharacterController controller;
    private float headYaw;
    private float pitch;
    private Vector3 pivotBasePos;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        pivotBasePos = cameraPivot.localPosition;
        headYaw = transform.eulerAngles.y;   // 從場景擺放的方向開始
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

        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            Look(mouse.delta.ReadValue());

        Vector2 moveInput = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) moveInput.y += 1f;
            if (kb.sKey.isPressed) moveInput.y -= 1f;
            if (kb.dKey.isPressed) moveInput.x += 1f;
            if (kb.aKey.isPressed) moveInput.x -= 1f;
        }

        Move(moveInput, Time.deltaTime);
        Hover();
    }

    public void Look(Vector2 delta)
    {
        Vector2 d = delta * mouseSensitivity;
        headYaw += d.x;
        pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, headYaw, 0f);         // 身體左右轉，每幀直接跟上滑鼠
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);    // 頭只上下看
    }

    public void Move(Vector2 move, float dt)
    {
        Vector3 direction = transform.rotation * new Vector3(move.x, 0f, move.y);
        Vector3 velocity = Vector3.ClampMagnitude(direction, 1f) * moveSpeed;

        controller.Move(velocity * dt);   // Y 是 0，所以高度不會變
    }

    void Hover()
    {
        float offset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        cameraPivot.localPosition = pivotBasePos + Vector3.up * offset;
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}