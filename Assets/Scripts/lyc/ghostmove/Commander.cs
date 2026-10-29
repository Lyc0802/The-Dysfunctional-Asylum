
using UnityEngine;

[RequireComponent(typeof(VehicleCharacterMotor))]
public class Commander : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.1f;
    public float maxHeadYaw = 100f;
    public float minPitch = -60f;
    public float maxPitch = 60f;

    [Header("Wheelchair")]
    public float forwardSpeed = 2.5f;
    public float backwardSpeed = 1.2f;
    public float turnSpeed = 90f;

    private VehicleCharacterMotor motor;
    private float headYaw;
    private float pitch;

    private void Awake()
    {
        motor = GetComponent<VehicleCharacterMotor>();
    }

    public void Look(Vector2 delta)
    {
        if (cameraPivot == null) return;

        Vector2 d = delta * mouseSensitivity;

        headYaw = Mathf.Clamp(headYaw + d.x, -maxHeadYaw, maxHeadYaw);
        pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);

        cameraPivot.localRotation = Quaternion.Euler(pitch, headYaw, 0f);
    }

    public void Drive(Vector2 move, float dt)
    {
        float throttle = move.y;
        float steer = move.x;

        Quaternion rotation = transform.rotation * Quaternion.Euler(0f, steer * turnSpeed * dt, 0f);

        float speed = throttle >= 0f ? forwardSpeed : backwardSpeed;
        Vector3 movement = rotation * Vector3.forward * throttle * speed * dt;

        motor.Move(movement, rotation, dt);
    }
}
