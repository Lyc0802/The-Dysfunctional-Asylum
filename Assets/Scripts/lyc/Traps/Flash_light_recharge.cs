using UnityEngine;
using UnityEngine.Events;



public class ShakeRechargeFlashlight : MonoBehaviour
{
    private Light targetLight;
    private bool light_check = false;

    [Header("追蹤位置：建議指定手電筒握把上的空物件")]
    [SerializeField] private Transform trackingPoint;

    [Header("充電設定")]
    [SerializeField, Range(0f, 100f)]
    private float battery = 0f;

    [SerializeField] private float chargePerShake = 5f;

    [Header("甩動條件")]
    [Tooltip("有效甩動的最低速度，單位：公尺/秒")]
    [SerializeField] private float minimumSpeed = 0.8f;

    [Tooltip("每段甩動至少需要移動的距離，單位：公尺")]
    [SerializeField] private float minimumStrokeDistance = 0.08f;

    [Tooltip("兩次充電之間的最短時間")]
    [SerializeField] private float chargeCooldown = 0.2f;

    [Header("事件")]
    public UnityEvent onShakeCharge;
    public UnityEvent<float> onBatteryChanged;

    public float Battery => battery;

    private bool isHeld;
    private bool hasStroke;

    private Vector3 lastPosition;
    private Vector3 strokeStart;
    private Vector3 strokeDirection;

    private float lastChargeTime;

    private Transform TrackingPoint =>
        trackingPoint != null ? trackingPoint : transform;

    // 綁定 Meta XR 的 When Select
    public void BeginHold()
    {
        isHeld = true;
        ResetTracking();
        light_check = true;
    }

    // 綁定 Meta XR 的 When Unselect 和 When Cancel
    public void EndHold()
    {
        isHeld = false;
        hasStroke = false;
        light_check = false;
    }

    private void OnDisable()
    {
        EndHold();
    }

    private void ResetTracking()
    {
        lastPosition = TrackingPoint.position;
        strokeStart = lastPosition;
        hasStroke = false;
        lastChargeTime = Time.time - chargeCooldown;
    }

    private void LateUpdate()
    {
        if (!isHeld || Time.deltaTime <= 0f)
            return;

        Vector3 position = TrackingPoint.position;
        Vector3 velocity =
            (position - lastPosition) / Time.deltaTime;

        lastPosition = position;

        if (velocity.magnitude < minimumSpeed)
            return;

        Vector3 direction = velocity.normalized;

        // 第一段快速移動只記錄方向，不直接充電。
        if (!hasStroke)
        {
            hasStroke = true;
            strokeDirection = direction;
            strokeStart = position;
            return;
        }

        // 點積小於 -0.5，代表移動方向已明顯反轉。
        bool reversed =
            Vector3.Dot(strokeDirection, direction) < -0.5f;

        if (!reversed)
            return;

        float strokeDistance = Mathf.Abs(
            Vector3.Dot(position - strokeStart, strokeDirection)
        );

        bool enoughDistance =
            strokeDistance >= minimumStrokeDistance;

        bool cooldownFinished =
            Time.time - lastChargeTime >= chargeCooldown;

        // 每次換向都重新開始記錄下一段。
        strokeStart = position;
        strokeDirection = direction;

        if (!enoughDistance || !cooldownFinished)
            return;

        if (battery >= 100f)
            return;

        battery = Mathf.Clamp(
            battery + chargePerShake, 0f, 100f
        );

        lastChargeTime = Time.time;

        onShakeCharge.Invoke();
        onBatteryChanged.Invoke(battery);
    }

    private void Update()
    {
        if (light_check && battery > 0)
        {
            targetLight.intensity = 50;
            battery -= (10 / 3) * Time.deltaTime;
        }
        else
        {
            targetLight.intensity = 0;
        }
    }

}

