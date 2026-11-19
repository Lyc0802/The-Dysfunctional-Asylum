using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Freddy : MonoBehaviour
{
    [Header("視角（頭）")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.1f;
    public float minPitch = -60f;
    public float maxPitch = 60f;

    [Header("移動")]
    public float moveSpeed = 3f;
    public float gravity = -20f;

    [Header("攻擊")]
    public float attackRange = 1.5f;        // 攻擊距離（從身體往前）
    public float attackRadius = 1f;         // 判定範圍半徑
    public float attackCooldown = 1f;       // 攻擊間隔（秒）
    public LayerMask attackLayers = ~0;     // 可以被打到的 Layer
    public Animator animator;               // 選填：有的話會觸發 "Attack" trigger

    private CharacterController controller;
    private float nextAttackTime;
    private float headYaw;
    private float pitch;
    private float verticalVelocity;

    void Awake()
    {
        
        controller = GetComponent<CharacterController>();

        // 自動抓子物件（模型）上的 Animator，並關掉 Root Motion：
        // 位移一律交給 CharacterController，動畫只負責動作
        if (animator == null) animator = GetComponentInChildren<Animator>();
        foreach (var a in GetComponentsInChildren<Animator>())
            a.applyRootMotion = false;
        if (animator == null) Debug.LogWarning("Freddy：找不到 Animator，動畫不會播放");

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
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked) TryAttack();   // 已鎖定 → 攻擊
            else LockCursor(true);                                         // 未鎖定 → 先鎖滑鼠
        }

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
    }

    public void Look(Vector2 delta)
    {
        Vector2 d = delta * mouseSensitivity;
        headYaw += d.x;
        pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, headYaw, 0f);         // 身體左右轉
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f); // 頭只上下看
    }

    public void Move(Vector2 move, float dt)
    {
        // 參數名稱要和 Animator 視窗裡的一模一樣（大小寫也要相同）：move
        animator.SetBool("move", move != Vector2.zero);
        Vector3 direction = transform.rotation * new Vector3(move.x, 0f, move.y);
        Vector3 velocity = Vector3.ClampMagnitude(direction, 1f) * moveSpeed;

        // 重力（保持貼地）
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * dt;
        velocity.y = verticalVelocity;

        controller.Move(velocity * dt);
    }

    public void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        animator.SetTrigger("attack");
        Vector3 center = AttackCenter();
        Collider[] hits = Physics.OverlapSphere(center, attackRadius, attackLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;   // 不打到自己
            
            hit.SendMessageUpwards("Die", SendMessageOptions.DontRequireReceiver);
            Debug.Log($"Freddy 殺死 {hit.name}");
        }
    }

    Vector3 AttackCenter()
    {
        return transform.position + Vector3.up * (controller != null ? controller.height * 0.5f : 1f)
               + transform.forward * attackRange;
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
