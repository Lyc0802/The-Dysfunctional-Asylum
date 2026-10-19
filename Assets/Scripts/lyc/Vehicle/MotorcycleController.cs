using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;

public class MotorcycleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handlebar;
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private FirstPersonLocomotor locomotor;
    [SerializeField] private Rigidbody rb;

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

    public float CurrentSpeed { get; private set; }
    public float Throttle { get; private set; }
    public float Steering { get; private set; }

    private Quaternion _initialRotation = new Quaternion(0.707107f, 0.707107f, 0f, 0f);
    private Quaternion _motorcycleRotation;

    private void Start()
    {
        _initialRotation = handlebar.localRotation;
        _motorcycleRotation = transform.rotation;
    }

    public void UpdateHandlebar(float dt)
    {
        bool isGrabbed =
            grabbable.SelectingPointsCount > 0 ;

        // Reset handlebar when released
        if (!isGrabbed)
        {
            Quaternion relative =
                Quaternion.Inverse(_initialRotation) *
                handlebar.localRotation;

            Vector3 angles = relative.eulerAngles;

            float x = Mathf.DeltaAngle(0f, angles.x);
            float y = Mathf.DeltaAngle(0f, angles.y);

            x = resetSteering
                ? Mathf.MoveTowards(x, 0f, resetSpeed * dt)
                : x;

            y = Mathf.MoveTowards(y, 0f, resetSpeed * dt);

            handlebar.localRotation =
                _initialRotation * Quaternion.Euler(x, y, 0f);
        }
    }
    // for local
    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        UpdateHandlebar(dt);

        // for local run 之後要刪除

        // Read local rotation relative to initial pose
        Quaternion relativeRotation =
            Quaternion.Inverse(_initialRotation) *
            handlebar.localRotation;

        Vector3 euler = relativeRotation.eulerAngles;

        float steeringAngle =
            Mathf.DeltaAngle(0f, euler.x);

        float throttleAngle =
            Mathf.DeltaAngle(0f, euler.y);        
            

        // Y: 0 ~ 90 -> 0 ~ 1
        Throttle = Mathf.Clamp01(
            -throttleAngle / maxThrottleAngle
        );

        // X: -35 ~ 35 -> -1 ~ 1
        Steering = Mathf.Clamp(
            steeringAngle / maxSteeringAngle,
            -1f,
            1f
        );

        // Update speed
        float targetSpeed = Throttle * maxSpeed;

        float speedChange = targetSpeed > CurrentSpeed
            ? acceleration
            : deceleration;

        CurrentSpeed = Mathf.MoveTowards(
            CurrentSpeed,
            targetSpeed,
            speedChange * dt
        );

        // Movement
        MoveMotorcycle(dt);
    }

    private void MoveMotorcycle(float dt)
    {
        if (CurrentSpeed <= 0f)
            return;
        // Debug.Log(CurrentSpeed);
        float distance = CurrentSpeed * dt;

        // Steering affects turning only while moving
        float turnAngle =
            Steering * maxTurnSpeed * dt;

        Quaternion deltaRotation =
            Quaternion.Euler(0f, turnAngle, 0f);

        Quaternion midpointRotation =
            _motorcycleRotation *
            Quaternion.Euler(0f, turnAngle * 0.5f, 0f);

        Vector3 movement =
            midpointRotation * Vector3.forward * distance;

        _motorcycleRotation *= deltaRotation;

        var locomotionEvent = new LocomotionEvent(
            0,
            new Pose(movement, deltaRotation),
            LocomotionEvent.TranslationType.Relative,
            LocomotionEvent.RotationType.Relative
        );

        locomotor.HandleLocomotionEvent(locomotionEvent);
    }

    // for multiplayer
    public void MoveMotorcycle(float dt, Quaternion handlebarLocalRotation)
    {
        // Read rotation relative to initial pose
        Quaternion relativeRotation = Quaternion.Inverse(_initialRotation) * handlebarLocalRotation;
        Vector3 euler = relativeRotation.eulerAngles;

        float steeringAngle = Mathf.DeltaAngle(0f, euler.x);
        float throttleAngle = Mathf.DeltaAngle(0f, euler.y);

        // Calculate throttle and steering
        Throttle = Mathf.Clamp01(-throttleAngle / maxThrottleAngle);
        Steering = Mathf.Clamp(steeringAngle / maxSteeringAngle, -1f, 1f);
        Debug.Log("Motor Throttle: "+Throttle);
        Debug.Log("Motor Steering: "+Steering);

        // Update speed
        float targetSpeed = Throttle * maxSpeed;
        float speedChange = targetSpeed > CurrentSpeed ? acceleration : deceleration;
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, speedChange * dt);

        if (CurrentSpeed <= 0f) return;

        // Calculate movement
        float distance = CurrentSpeed * dt;
        float turnAngle = Steering * maxTurnSpeed * dt;

        Quaternion deltaRotation = Quaternion.Euler(0f, turnAngle, 0f);
        Quaternion midpointRotation = rb.rotation * Quaternion.Euler(0f, turnAngle * 0.5f, 0f);

        Vector3 movement = midpointRotation * Vector3.forward * distance;

        // // Apply Rigidbody movement
        // rb.MovePosition(rb.position + movement);
        // rb.MoveRotation(rb.rotation * deltaRotation);
    }

}