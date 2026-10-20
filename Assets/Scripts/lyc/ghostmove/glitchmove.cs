using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 幽靈般的第一人稱漂浮移動（v2）
/// - 與地面保持固定距離，地形起伏時平滑升降
/// - 移動有慣性，像在空中滑行
/// - 上下緩慢浮動、側移傾斜、前進前傾、轉頭傾斜、隨機漂移
/// - 使用 CharacterController，不會穿牆
/// v2 修正：
/// - 同時支援新舊輸入系統
/// - 攝影機不必是子物件，腳本會自動帶著它走
/// - 地面偵測會自動忽略自己的碰撞體，不用設定圖層
/// - 開始時自動把角色放到正確高度，避免卡在地板裡
/// - 畫面左上角顯示除錯資訊
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class GhostController : MonoBehaviour
{
    [Header("參考")]
    [Tooltip("攝影機，留空自動抓 Camera.main。不需要是子物件")]
    public Transform cameraTransform;

    [Header("移動")]
    public float moveSpeed = 3f;
    [Tooltip("加減速的平滑時間，越大越飄、慣性越重")]
    public float moveSmoothTime = 0.35f;

    [Header("漂浮")]
    [Tooltip("與地面的固定距離（攝影機高度）")]
    public float hoverHeight = 1.6f;
    [Tooltip("地形高低變化時，高度跟上的平滑時間")]
    public float heightSmoothTime = 0.4f;
    [Tooltip("上下浮動的幅度（公尺）")]
    public float bobAmplitude = 0.08f;
    [Tooltip("上下浮動的頻率（次/秒）")]
    public float bobFrequency = 0.5f;
    public float groundCheckDistance = 20f;
    public LayerMask groundLayers = ~0;

    [Header("視角")]
    public float mouseSensitivity = 2f;
    public float maxPitch = 85f;

    [Header("畫面搖晃")]
    public float strafeRoll = 4f;
    public float forwardPitch = 2f;
    public float turnRoll = 0.8f;
    public float tiltSmooth = 4f;
    public float driftAmount = 0.6f;
    public float driftSpeed = 0.25f;

    [Header("除錯")]
    [Tooltip("在畫面左上角顯示輸入與位置資訊，確認正常後可以關掉")]
    public bool showDebug = true;

    CharacterController cc;
    Vector3 velocity, velocityRef;
    float heightVelocityRef;
    float groundY;
    bool groundFound;
    float yaw, pitch;
    float currentRoll, currentPitchTilt;
    float seed;

    Vector2 moveInput, lookInput;
    bool escapePressed, clickPressed;
    string inputSystemName;

    readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        yaw = transform.eulerAngles.y;
        seed = Random.value * 100f;
        groundY = transform.position.y - hoverHeight;

#if ENABLE_INPUT_SYSTEM
        inputSystemName = "New Input System";
#else
        inputSystemName = "Old Input Manager";
#endif
    }

    void Start()
    {
        // 從上方往下找地面，直接把角色放到正確高度，避免一開始卡在地板裡
        if (FindGround(transform.position + Vector3.up * 3f, groundCheckDistance + 3f, out float y))
        {
            groundY = y;
            cc.enabled = false;
            transform.position = new Vector3(transform.position.x, y + hoverHeight, transform.position.z);
            cc.enabled = true;
        }
        SetCursorLocked(true);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        ReadInput();

        if (escapePressed) SetCursorLocked(false);
        else if (clickPressed) SetCursorLocked(true);

        // ── 視角 ──
        yaw += lookInput.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, -maxPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // ── 水平移動（帶慣性）──
        Vector3 input = transform.right * moveInput.x + transform.forward * moveInput.y;
        input = Vector3.ClampMagnitude(input, 1f);
        velocity = Vector3.SmoothDamp(velocity, input * moveSpeed, ref velocityRef, moveSmoothTime);

        // ── 離地高度 ──
        groundFound = FindGround(transform.position, groundCheckDistance, out float gy);
        if (groundFound) groundY = gy;

        float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        float targetY = groundY + hoverHeight + bob;
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref heightVelocityRef, heightSmoothTime);
        float deltaY = newY - transform.position.y;

        // ── 套用移動（CharacterController 負責擋牆）──
        cc.Move(velocity * dt + Vector3.up * deltaY);
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;
        float dt = Time.deltaTime;

        Vector3 localVel = transform.InverseTransformDirection(velocity) / Mathf.Max(moveSpeed, 0.01f);

        float targetRoll = -localVel.x * strafeRoll
                         - Mathf.Clamp(lookInput.x * mouseSensitivity * turnRoll, -strafeRoll, strafeRoll);
        float targetPitchTilt = localVel.z * forwardPitch;

        float t = 1f - Mathf.Exp(-tiltSmooth * dt);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, t);
        currentPitchTilt = Mathf.Lerp(currentPitchTilt, targetPitchTilt, t);

        float time = Time.time * driftSpeed;
        float nx = (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f * driftAmount;
        float ny = (Mathf.PerlinNoise(seed + 10f, time) - 0.5f) * 2f * driftAmount;
        float nz = (Mathf.PerlinNoise(seed + 20f, time) - 0.5f) * 2f * driftAmount;

        // 用世界座標直接設定，攝影機是不是子物件都沒關係
        cameraTransform.SetPositionAndRotation(
            transform.position,
            Quaternion.Euler(pitch + currentPitchTilt + nx, yaw + ny, currentRoll + nz));
    }

    // ═════════ 輸入（新舊系統都支援）═════════
    void ReadInput()
    {
        bool locked = Cursor.lockState == CursorLockMode.Locked;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        Vector2 m = Vector2.zero;
        escapePressed = clickPressed = false;

        if (kb != null)
        {
            m.x = ((kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1f : 0f)
                - ((kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? 1f : 0f);
            m.y = ((kb.wKey.isPressed || kb.upArrowKey.isPressed) ? 1f : 0f)
                - ((kb.sKey.isPressed || kb.downArrowKey.isPressed) ? 1f : 0f);
            escapePressed = kb.escapeKey.wasPressedThisFrame;
        }
        if (mouse != null)
        {
            clickPressed = mouse.leftButton.wasPressedThisFrame;
            // 新系統的滑鼠位移是像素，乘 0.1 讓靈敏度接近舊系統
            lookInput = locked ? mouse.delta.ReadValue() * 0.1f : Vector2.zero;
        }
        else lookInput = Vector2.zero;

        moveInput = Vector2.ClampMagnitude(m, 1f);
#else
        moveInput = Vector2.ClampMagnitude(
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        lookInput = locked ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) : Vector2.zero;
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
        clickPressed = Input.GetMouseButtonDown(0);
#endif
    }

    // ═════════ 地面偵測（自動忽略自己）═════════
    bool FindGround(Vector3 origin, float distance, out float y)
    {
        y = 0f;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuffer, distance,
                                            groundLayers, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            var h = hitBuffer[i];
            if (h.collider.transform.IsChildOf(transform)) continue;   // 跳過自己和子物件
            if (cameraTransform != null && h.collider.transform.IsChildOf(cameraTransform)) continue;
            if (h.distance < best)
            {
                best = h.distance;
                y = h.point.y;
                found = true;
            }
        }
        return found;
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnGUI()
    {
        if (!showDebug) return;
        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, 10, 600, 140),
            $"Input: {inputSystemName}\n" +
            $"Move input: {moveInput}   Look input: {lookInput}\n" +
            $"Velocity: {velocity.magnitude:F2}\n" +
            $"Position: {transform.position}\n" +
            $"Ground: {(groundFound ? groundY.ToString("F2") : "NOT FOUND")}\n" +
            $"Camera: {(cameraTransform != null ? cameraTransform.name : "NONE")}   Cursor: {Cursor.lockState}");
    }
}