using UnityEngine;
using UnityEngine.Events;

public class YSlideRecharge : MonoBehaviour
{
    [Header("滑動物件")]
    [SerializeField] private Transform slider;

    [Header("充電設定")]
    [SerializeField, Range(0f, 100f)]
    private float battery = 0f;

    [Tooltip("超過這個速度才充電，單位為本地座標單位/秒")]
    [SerializeField, Min(0.01f)]
    private float minimumSpeed = 0.8f;

    [Tooltip("有效滑動每 1 單位距離增加多少電量")]
    [SerializeField, Min(0f)]
    private float chargePerUnit = 20f;

    [Header("充電回饋")]
    [Tooltip("充電音效等回饋的最短觸發間隔")]
    [SerializeField, Min(0f)]
    private float feedbackInterval = 0.2f;

    public UnityEvent onCharge;
    public UnityEvent<float> onBatteryChanged;

    public float Battery => battery;

    private bool isHeld;
    private Vector3 previousPosition;
    private float nextFeedbackTime;

    private Transform Slider =>
        slider != null ? slider : transform;

    // 綁定 When Select
    public void BeginHold()
    {
        isHeld = true;
        previousPosition = Slider.position;
        nextFeedbackTime = Time.time;
    }

    // 綁定 When Unselect 與 When Cancel
    public void EndHold()
    {
        isHeld = false;
    }

    private void LateUpdate()
    {
        if (!isHeld || Time.deltaTime <= 0f)
            return;

        Vector3 currentPosition = Slider.position;
        Vector3 movement = currentPosition - previousPosition;

        // Slider.up 是物件旋轉後，自身 Y 軸在世界中的方向。
        float distance = Mathf.Abs(
            Vector3.Dot(movement, Slider.up)
        );

        previousPosition = currentPosition;

        float speed = distance / Time.deltaTime;

        if (speed < minimumSpeed || battery >= 100f)
            return;

        // 依實際有效滑動距離充電，不依幀數充電。
        float newBattery = Mathf.Clamp(
            battery + distance * chargePerUnit,
            0f,
            100f
        );

        if (newBattery <= battery)
            return;

        battery = newBattery;
        onBatteryChanged.Invoke(battery);

        // 回饋事件限頻，避免音效每幀觸發。
        if (Time.time >= nextFeedbackTime)
        {
            nextFeedbackTime = Time.time + feedbackInterval;
            onCharge.Invoke();
        }
    }

    private void OnDisable()
    {
        isHeld = false;
    }
}