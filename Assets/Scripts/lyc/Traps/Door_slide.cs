using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class Door_slide : MonoBehaviour, ITransformer
{
    [Header("Door handles (children of the moving door)")]
    [SerializeField] private Transform handleA;
    [SerializeField] private Transform handleB;

    [Header("Slide (world units and units/second)")]
    [SerializeField, Min(0f)] private float maxSlideDistance = 2f;
    [SerializeField, Min(0f)] private float maxSlideSpeed = 1f;
    [Header("Return after both handles are released")]
    [SerializeField] private bool autoReturn = true;
    [SerializeField, Min(0f)] private float returnSpeed = 0.5f;

    public float SlideDistance => slideDistance;
    public bool IsHandleAHeld => grabA != null && grabA.GrabPoints.Count > 0;
    public bool IsHandleBHeld => grabB != null && grabB.GrabPoints.Count > 0;

    private Grabbable grabA;
    private Grabbable grabB;
    private Vector3 topPosition;
    private Vector3 downDirection;
    private Vector3 startA;
    private Vector3 startB;
    private float startDistance;
    private float slideDistance;
    private bool trackingReady;

    private void Awake()
    {
        topPosition = transform.position;
        downDirection = -transform.up.normalized;
        if (handleA == null || handleB == null || handleA == handleB ||
            !handleA.IsChildOf(transform) || !handleB.IsChildOf(transform))
        {
            Debug.LogError("Assign two different door handle children on Door_slide.", this);
            enabled = false;
            return;
        }
        grabA = ConfigureHandle(handleA);
        grabB = ConfigureHandle(handleB);
    }

    private Grabbable ConfigureHandle(Transform handle)
    {
        Rigidbody body = handle.GetComponent<Rigidbody>();
        if (body == null) body = handle.gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        Grabbable grab = handle.GetComponent<Grabbable>();
        if (grab == null) grab = handle.gameObject.AddComponent<Grabbable>();
        grab.MaxGrabPoints = 1;
        // The door owns movement; neither handle may move independently.
        grab.InjectOptionalOneGrabTransformer(this);
        grab.InjectOptionalTwoGrabTransformer(this);

        GrabInteractable controller = handle.GetComponent<GrabInteractable>();
        if (controller == null) controller = handle.gameObject.AddComponent<GrabInteractable>();
        controller.InjectRigidbody(body);
        controller.InjectOptionalPointableElement(grab);
        HandGrabInteractable hand = handle.GetComponent<HandGrabInteractable>();
        if (hand == null) hand = handle.gameObject.AddComponent<HandGrabInteractable>();
        hand.InjectRigidbody(body);
        hand.InjectOptionalPointableElement(grab);
        return grab;
    }

    private void LateUpdate()
    {
        bool heldA = IsHandleAHeld;
        bool heldB = IsHandleBHeld;
        if (heldA && heldB)
        {
            Vector3 currentA = grabA.GrabPoints[0].position;
            Vector3 currentB = grabB.GrabPoints[0].position;
            if (!trackingReady)
            {
                startA = currentA;
                startB = currentB;
                startDistance = slideDistance;
                trackingReady = true;
            }
            // Compare total travel, so the hands need not move on the exact same frame.
            float travel = Mathf.Min(Vector3.Dot(currentA - startA, downDirection),
                Vector3.Dot(currentB - startB, downDirection));
            float target = Mathf.Clamp(startDistance + travel, slideDistance, Mathf.Max(slideDistance, maxSlideDistance));
            slideDistance = Mathf.MoveTowards(slideDistance, target, Mathf.Max(0f, maxSlideSpeed) * Time.deltaTime);
        }
        else
        {
            trackingReady = false;
            if (!heldA && !heldB && autoReturn)
                slideDistance = Mathf.MoveTowards(slideDistance, 0f, Mathf.Max(0f, returnSpeed) * Time.deltaTime);
        }
        transform.position = topPosition + downDirection * slideDistance;
    }

    public void EnableAutoReturn() => autoReturn = true;
    public void DisableAutoReturn() => autoReturn = false;
    public void SetAutoReturn(bool enabled) => autoReturn = enabled;
    private void OnDisable() => trackingReady = false;

    // No-op transformers keep the handles fixed to the door while exposing grab poses.
    public void Initialize(IGrabbable grabbable) { }
    public void BeginTransform() { }
    public void UpdateTransform() { }
    public void EndTransform() { }
}
