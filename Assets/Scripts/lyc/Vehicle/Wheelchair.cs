using UnityEngine;

public class Wheelchair : MonoBehaviour
{
    [Header("Wheels")]
    [SerializeField] private WheelchairWheel leftWheel;
    [SerializeField] private WheelchairWheel rightWheel;

    [Header("Movement")]
    [SerializeField] private VehicleCharacterMotor motor;
    [SerializeField] private float wheelSpacing = 0.8f;
    [SerializeField] private float moveMultiplier = 1f;
    [SerializeField] private float speed = 10f;

    private void Awake()
    {
        if (motor == null) motor = GetComponent<VehicleCharacterMotor>();
    }

    public Vector2 GetWheelDistance()
    {
        return new Vector2(leftWheel.GetDeltaDistance(), rightWheel.GetDeltaDistance());
    }

    public void MoveWheelchair(Vector2 wheelDistance, float dt)
    {
        float leftDistance = wheelDistance.x * moveMultiplier;
        float rightDistance = wheelDistance.y * moveMultiplier;

        float distance = (leftDistance + rightDistance) * 0.5f * speed;
        float rotation = (rightDistance - leftDistance) / wheelSpacing;
        float rotationDegrees = rotation * Mathf.Rad2Deg;

        Quaternion currentRotation = transform.rotation;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotationDegrees, 0f);
        Quaternion midpointRotation = currentRotation * Quaternion.Euler(0f, rotationDegrees * 0.5f, 0f);

        Vector3 movement = midpointRotation * Vector3.forward * distance;

        motor.Move(movement, currentRotation * deltaRotation, dt);
    }
}