using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class WheelchairController : MonoBehaviour
{
    [Header("視角（頭）")]
    public Transform cameraPivot;               // 拖入角色底下的相機
    public float mouseSensitivity = 0.1f;
    public float maxHeadYaw = 100f;             // 頭最多往左右轉幾度
    public float minPitch = -60f;
    public float maxPitch = 60f;

    [Header("輪椅")]
    public float forwardSpeed = 2.5f;
    public float backwardSpeed = 1.2f;          // 倒退比較慢
    public float turnSpeed = 90f;               // 每秒轉幾度

    [Header("重力")]
    public float gravity = -20f;

    CharacterController controller;
    float headYaw;
    float pitch;
    float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
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

        Look(mouse);
        Drive(kb);
    }

    void Look(Mouse mouse)
    {
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 d = mouse.delta.ReadValue() * mouseSensitivity;
            headYaw = Mathf.Clamp(headYaw + d.x, -maxHeadYaw, maxHeadYaw);
            pitch   = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
        }

        // 頭相對於輪椅轉動，輪椅本身不受滑鼠影響
        cameraPivot.localRotation = Quaternion.Euler(pitch, headYaw, 0f);
    }

    void Drive(Keyboard kb)
    {
        float throttle = 0f;   // 前後
        float steer = 0f;      // 轉向

        if (kb != null)
        {
            if (kb.wKey.isPressed) throttle += 1f;
            if (kb.sKey.isPressed) throttle -= 1f;
            if (kb.dKey.isPressed) steer += 1f;
            if (kb.aKey.isPressed) steer -= 1f;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            throttle += stick.y;
            steer += stick.x;
        }

        throttle = Mathf.Clamp(throttle, -1f, 1f);
        steer = Mathf.Clamp(steer, -1f, 1f);

        // 轉向：轉動整台輪椅
        transform.Rotate(Vector3.up, steer * turnSpeed * Time.deltaTime);

        // 前進或後退：只沿著輪椅的正前方
        float speed = throttle >= 0f ? forwardSpeed : backwardSpeed;
        Vector3 move = transform.forward * throttle * speed;

        // 重力
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}