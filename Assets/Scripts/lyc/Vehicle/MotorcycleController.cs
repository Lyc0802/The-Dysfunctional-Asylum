
using UnityEngine;
using Oculus.Interaction;

public class MotorcycleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handlebar;
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private VehicleCharacterMotor motor;

    [Header("Speed")]
    [SerializeField] private float maxThrottleAngle = 90f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 12f;

    [Header("Steering")]
    [SerializeField] private float maxSteeringAngle = 35f;
    [SerializeField] private float maxTurnSpeed = 90f;

    [Header("Handlebar Reset")]
    [SerializeField] private float resetSpeed = 180f;
    [SerializeField] private bool resetSteering = true;

    public float Throttle { get; private set; }
    public float Steering { get; private set; }

    private Quaternion _initialRotation = new Quaternion(0.707107f, 0.707107f, 0f, 0f);

    private void Awake()
    {
        if (motor == null) motor = GetComponent<VehicleCharacterMotor>();
        if (handlebar != null) _initialRotation = handlebar.localRotation;
    }

    private void LateUpdate()
    {
        if (grabbable == null || handlebar == null) return;
        UpdateHandlebar(Time.deltaTime);
    }

    public void UpdateHandlebar(float dt)
    {
        if (grabbable.SelectingPointsCount > 0) return;

        Quaternion relative = Quaternion.Inverse(_initialRotation) * handlebar.localRotation;
        Vector3 angles = relative.eulerAngles;

        float x = Mathf.DeltaAngle(0f, angles.x);
        float y = Mathf.DeltaAngle(0f, angles.y);

        if (resetSteering)
            x = Mathf.MoveTowards(x, 0f, resetSpeed * dt);

        y = Mathf.MoveTowards(y, 0f, resetSpeed * dt);

        handlebar.localRotation = _initialRotation * Quaternion.Euler(x, y, 0f);
    }

    public float MoveMotorcycle(float dt, Quaternion handlebarLocalRotation, float currentSpeed)
    {
        Quaternion relativeRotation = Quaternion.Inverse(_initialRotation) * handlebarLocalRotation;
        Vector3 euler = relativeRotation.eulerAngles;

        float steeringAngle = Mathf.DeltaAngle(0f, euler.x);
        float throttleAngle = Mathf.DeltaAngle(0f, euler.y);

        Throttle = Mathf.Clamp01(-throttleAngle / maxThrottleAngle);
        Steering = Mathf.Clamp(steeringAngle / maxSteeringAngle, -1f, 1f);

        float targetSpeed = Throttle * maxSpeed;
        float speedChange = targetSpeed > currentSpeed ? acceleration : deceleration;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChange * dt);

        float distance = currentSpeed * dt;
        float turnAngle = Steering * maxTurnSpeed * dt;

        Quaternion currentRotation = transform.rotation;
        Quaternion deltaRotation = Quaternion.Euler(0f, turnAngle, 0f);
        Quaternion midpointRotation = currentRotation * Quaternion.Euler(0f, turnAngle * 0.5f, 0f);

        Vector3 movement = midpointRotation * Vector3.forward * distance;

        motor.Move(movement, currentRotation * deltaRotation, dt);

        return currentSpeed;
    }
}
