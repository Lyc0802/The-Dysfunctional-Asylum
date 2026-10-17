using UnityEngine;
using Oculus.Interaction;

public class TwoHandleSlide : MonoBehaviour, ITransformer
{
    [Header("手部追蹤點：請指定實際的手或控制器")]
    [SerializeField] private Transform handA;
    [SerializeField] private Transform handB;

    [Header("滑動設定（公尺、公尺/秒）")]
    [SerializeField, Min(0f)]
    private float maxSlideDistance = 2f;

    [SerializeField, Min(0f)]
    private float maxSlideSpeed = 1f;

    [Header("放開後自動上升")]
    [SerializeField]
    private bool autoReturn = true;

    [SerializeField, Min(0f)]
    private float returnSpeed = 0.5f;

    public float SlideDistance => slideDistance;

    private bool heldA;
    private bool heldB;

    private Vector3 topPosition;
    private Vector3 downDirection;

    private Vector3 previousHandA;
    private Vector3 previousHandB;

    private float slideDistance;
    private bool trackingReady;

    private void Awake()
    {
        // 初始位置是上限。
        // 使用物件啟動時的本地向下方向作為滑軌方向。
        topPosition = transform.position;
        downDirection = -transform.up.normalized;
    }

    private void LateUpdate()
    {
        if (heldA && heldB)
        {
            UpdateDownwardSlide();
        }
        else
        {
            trackingReady = false;

            // 只有兩個手把都放開時才自動上升。
            if (!heldA && !heldB && autoReturn)
            {
                slideDistance = Mathf.MoveTowards(
                    slideDistance,
                    0f,
                    returnSpeed * Time.deltaTime
                );
            }
        }

        transform.position =
            topPosition + downDirection * slideDistance;
    }

    private void UpdateDownwardSlide()
    {
        if (handA == null || handB == null)
        {
            trackingReady = false;
            return;
        }

        Vector3 currentA = handA.position;
        Vector3 currentB = handB.position;

        if (!trackingReady)
        {
            previousHandA = currentA;
            previousHandB = currentB;
            trackingReady = true;
            return;
        }

        // 計算兩手沿滑軌向下移動的距離。
        float movementA = Vector3.Dot(
            currentA - previousHandA,
            downDirection
        );

        float movementB = Vector3.Dot(
            currentB - previousHandB,
            downDirection
        );

        previousHandA = currentA;
        previousHandB = currentB;

        // 任一手沒有往下移動，就不下降。
        if (movementA <= 0f || movementB <= 0f)
            return;

        // 跟隨移動較少的那隻手。
        float downwardMovement =
            Mathf.Min(movementA, movementB);

        // 限制每幀可下降的最大距離。
        float allowedMovement =
            maxSlideSpeed * Time.deltaTime;

        slideDistance = Mathf.Clamp(
            slideDistance +
            Mathf.Min(downwardMovement, allowedMovement),
            0f,
            maxSlideDistance
        );
    }

    // ===== 綁定手把 A 的事件 =====

    public void GrabA()
    {
        heldA = true;
        trackingReady = false;
    }

    public void ReleaseA()
    {
        heldA = false;
        trackingReady = false;
    }

    // ===== 綁定手把 B 的事件 =====

    public void GrabB()
    {
        heldB = true;
        trackingReady = false;
    }

    public void ReleaseB()
    {
        heldB = false;
        trackingReady = false;
    }

    // ===== 自動上升開關 =====

    public void EnableAutoReturn()
    {
        autoReturn = true;
    }

    public void DisableAutoReturn()
    {
        autoReturn = false;
    }

    public void SetAutoReturn(bool enabled)
    {
        autoReturn = enabled;
    }

    private void OnDisable()
    {
        heldA = false;
        heldB = false;
        trackingReady = false;
    }

    // 作為兩個手把的「不移動 Transformer」。
    // 手把本身不跟手移動，由上面的程式控制整體位置。
    public void Initialize(IGrabbable grabbable) { }
    public void BeginTransform() { }
    public void UpdateTransform() { }
    public void EndTransform() { }
}