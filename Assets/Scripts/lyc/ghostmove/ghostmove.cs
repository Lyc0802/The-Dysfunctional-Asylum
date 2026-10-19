using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ghostmove : MonoBehaviour
{
    [Header("視角")]
    public Transform cameraPivot;                // 拖入 Main Camera（必須是子物件）
    public float mouseSensitivity = 0.1f;
    public float minPitch = -70f;
    public float maxPitch = 70f;

    [Header("重力")]
    public float gravity = -30f;

    [Header("步伐節奏")]
    public float strideLength = 5f;              // 一步跨多遠（公尺）
    public float stepDuration = 1.0f;            // 跨一步花幾秒
    public float plantPause = 0.8f;              // 落地後停頓幾秒才跨下一步

    [Header("落地衝擊")]
    public float bobHeight = 0.4f;               // 跨步時鏡頭抬高的幅度
    public float landingDrop = 0.3f;             // 落地瞬間鏡頭再往下沉
    public float landingDip = 5f;                // 落地時往下點頭（度）
    public float landingShake = 0.15f;           // 落地震動
    public float impactDecay = 3f;               // 衝擊消退速度（越小殘留越久）

    [Header("重心擺動")]
    public float swayAmount = 0.3f;              // 左右重心移動（公尺）
    public float rollAmount = 4f;                // 左右傾斜（度）

    CharacterController controller;
    Vector3 camDefaultPos;
    float pitch;
    float verticalVelocity;

    float walkBlend;        // 0~1，行走狀態（淡入淡出鏡頭效果）
    float stepPhase;        // 0 = 沒在跨步；0~1 = 跨步進度
    float pauseTimer;       // 落地停頓剩餘時間
    int stepIndex;          // 左右腳交替
    Vector3 stepDir;        // 這一步的方向
    float shake, dip, drop;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        camDefaultPos = cameraPivot.localPosition;
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
        Move(kb);
        CameraEffects();
    }

    void Look(Mouse mouse)
    {
        if (mouse == null || Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 d = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up, d.x);
        pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
    }

    void Move(Keyboard kb)
    {
        Vector2 m = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) m.y += 1;
            if (kb.sKey.isPressed) m.y -= 1;
            if (kb.dKey.isPressed) m.x += 1;
            if (kb.aKey.isPressed) m.x -= 1;
        }
        var pad = Gamepad.current;
        if (pad != null) m += pad.leftStick.ReadValue();
        m = Vector2.ClampMagnitude(m, 1f);

        bool hasInput = m.magnitude > 0.1f;
        Vector3 inputDir = transform.right * m.x + transform.forward * m.y;
        if (hasInput) stepDir = inputDir;   // 跨步中仍可微調方向

        Vector3 move = Vector3.zero;

        if (pauseTimer > 0f)
        {
            // 落地停頓：不移動
            pauseTimer -= Time.deltaTime;
        }
        else if (stepPhase > 0f || hasInput)
        {
            // 跨步中：一旦開始就會跨完這一步
            stepPhase += Time.deltaTime / stepDuration;
            float p = Mathf.Min(stepPhase, 1f);

            // 速度曲線：慢 → 快 → 慢，總距離剛好等於 strideLength
            float curve = Mathf.Sin(p * Mathf.PI) * Mathf.PI * 0.5f;
            move = stepDir * (strideLength / stepDuration) * curve;

            if (stepPhase >= 1f)
            {
                stepPhase = 0f;
                stepIndex++;
                pauseTimer = plantPause;
                OnFootstep();
            }
        }

        // 行走狀態：持續按著或還在跨步就算在走
        bool walking = hasInput || stepPhase > 0f;
        walkBlend = Mathf.MoveTowards(walkBlend, walking ? 1f : 0f, Time.deltaTime * 1.5f);

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    void OnFootstep()
    {
        float k = stepDir.magnitude;   // 搖桿輕推時衝擊較小
        dip   = landingDip   * k;
        shake = landingShake * k;
        drop  = landingDrop  * k;
    }

    void CameraEffects()
    {
        float lift = stepPhase > 0f ? Mathf.Sin(stepPhase * Mathf.PI) : 0f; // 跨步中抬高，停頓時為 0
        float side = (stepIndex % 2 == 0) ? 1f : -1f;

        // 跨步抬高 + 重心左右移動
        Vector3 offset = new Vector3(
            side * swayAmount * lift,
            (lift - 1f) * bobHeight,
            0f) * walkBlend;

        // 落地衝擊：下沉、點頭、震動，之後慢慢恢復
        float decay = Mathf.Exp(-impactDecay * Time.deltaTime);
        shake *= decay;
        dip   *= decay;
        drop  *= decay;

        float t = Time.time * 25f;
        offset += new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f,
                              Mathf.PerlinNoise(0f, t) - 0.5f, 0f) * 2f * shake;
        offset.y -= drop;

        float roll = side * rollAmount * lift * walkBlend;

        cameraPivot.localPosition = camDefaultPos + offset;
        cameraPivot.localRotation = Quaternion.Euler(pitch + dip, 0f, roll);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}