using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Runner : MonoBehaviour
{
    [Header("視角（頭）")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.1f;
    public float minPitch = -60f;
    public float maxPitch = 60f;

    [Header("移動")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float acceleration = 10f;        // 速度變化快慢
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    [Header("體力")]
    public float maxStamina = 5f;           // 可以連續跑幾秒
    public float staminaRecover = 1f;       // 每秒回復量
    public float staminaMinToRun = 1f;      // 跑到沒力後，要回復到多少才能再跑

    [Header("跑步晃動")]
    public float walkBobAmplitude = 0.04f;
    public float walkBobFrequency = 1.8f;
    public float runBobAmplitude = 0.08f;
    public float runBobFrequency = 2.8f;

    private CharacterController controller;
    private float headYaw;
    private float pitch;
    private Vector3 pivotBasePos;

    private float currentSpeed;
    private float verticalVelocity;
    private float stamina;
    private bool exhausted;
    private float bobTimer;

    public bool IsRunning { get; private set; }
    public float Stamina01 => stamina / maxStamina;   // 給 UI 用

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        pivotBasePos = cameraPivot.localPosition;
        headYaw = transform.eulerAngles.y;   // 從場景擺放的方向開始
        stamina = maxStamina;
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
        bool runHeld = false;
        bool jumpPressed = false;
        if (kb != null)
        {
            if (kb.wKey.isPressed) moveInput.y += 1f;
            if (kb.sKey.isPressed) moveInput.y -= 1f;
            if (kb.dKey.isPressed) moveInput.x += 1f;
            if (kb.aKey.isPressed) moveInput.x -= 1f;
            runHeld = kb.leftShiftKey.isPressed;
            jumpPressed = kb.spaceKey.wasPressedThisFrame;
        }

        Move(moveInput, runHeld, jumpPressed, Time.deltaTime);
        HeadBob(Time.deltaTime);
    }

    public void Look(Vector2 delta)
    {
        Vector2 d = delta * mouseSensitivity;
        headYaw += d.x;
        pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, headYaw, 0f);         // 身體左右轉
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);    // 頭只上下看
    }

    public void Move(Vector2 move, bool runHeld, bool jump, float dt)
    {
        bool grounded = controller.isGrounded;

        // 只有往前跑（有按 W）且有體力才算奔跑
        bool wantRun = runHeld && move.y > 0f && !exhausted;
        UpdateStamina(wantRun, dt);
        IsRunning = wantRun && !exhausted;

        // 水平移動：平滑加減速
        float targetSpeed = move == Vector2.zero ? 0f : (IsRunning ? runSpeed : walkSpeed);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * dt);

        Vector3 direction = transform.rotation * new Vector3(move.x, 0f, move.y);
        Vector3 velocity = Vector3.ClampMagnitude(direction, 1f) * currentSpeed;

        // 重力 / 跳躍
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;   // 貼地
        if (grounded && jump) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * dt;
        velocity.y = verticalVelocity;

        controller.Move(velocity * dt);
    }

    void UpdateStamina(bool running, float dt)
    {
        if (running)
        {
            stamina -= dt;
            if (stamina <= 0f)
            {
                stamina = 0f;
                exhausted = true;   // 跑到沒力
            }
        }
        else
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRecover * dt);
            if (exhausted && stamina >= staminaMinToRun) exhausted = false;
        }
    }

    void HeadBob(float dt)
    {
        bool moving = controller.isGrounded && currentSpeed > 0.1f;
        if (moving)
        {
            float freq = IsRunning ? runBobFrequency : walkBobFrequency;
            float amp = IsRunning ? runBobAmplitude : walkBobAmplitude;
            bobTimer += dt * freq * Mathf.PI * 2f;

            float y = Mathf.Sin(bobTimer) * amp;                 // 上下
            float x = Mathf.Cos(bobTimer * 0.5f) * amp * 0.5f;   // 左右（步伐）
            cameraPivot.localPosition = pivotBasePos + new Vector3(x, y, 0f);
        }
        else
        {
            bobTimer = 0f;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, pivotBasePos, dt * 8f);   // 停下時回正
        }
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
