using UnityEngine;
using Oculus.Interaction.Locomotion;

public class Wheelchair : MonoBehaviour
{
    [Header("Wheels")]
    [SerializeField] private WheelchairWheel leftWheel;
    [SerializeField] private WheelchairWheel rightWheel;

    [Header("Locomotion")]
    [SerializeField] private FirstPersonLocomotor locomotor;
    [SerializeField] private Rigidbody rb;

    [Header("Wheelchair Settings")]
    [SerializeField] private float wheelSpacing = 0.8f;
    [SerializeField] private float moveMultiplier = 1f;
    [SerializeField] private float speed = 10f;


    private void Update()
    {
        // for local run 之後要刪除
        // local player 
        locomotor.HandleLocomotionEvent(GetLocomotionEvent(GetWheelDistance()));
    }
    
    // for local run 之後要刪除
    public LocomotionEvent GetLocomotionEvent(Vector2 WheelDistance)
    {
        float leftDistance = WheelDistance.x;
        float rightDistance = WheelDistance.y;

        leftDistance *= moveMultiplier;
        rightDistance *= moveMultiplier;

        // Average distance of both wheels
        float distance = (leftDistance + rightDistance) * 0.5f * speed;

        // Differential rotation
        float rotation = (rightDistance - leftDistance) / wheelSpacing;
        
        // Delta rotation
        Quaternion deltaRotation =
            Quaternion.Euler(
                0f,
                rotation * Mathf.Rad2Deg,
                0f
            );

        // Midpoint heading
        Quaternion midpointRotation =
            locomotor.transform.rotation *
            Quaternion.Euler(
                0f,
                rotation * Mathf.Rad2Deg * 0.5f,
                0f
            );

        // World-space displacement
        Vector3 movement =
            midpointRotation * Vector3.forward * distance;
        // Debug.Log(movement)
        // Apply locomotion
        LocomotionEvent locomotionEvent = new LocomotionEvent(
            0,
            new Pose(movement, deltaRotation),
            LocomotionEvent.TranslationType.Relative,
            LocomotionEvent.RotationType.Relative
        );
        return locomotionEvent;
    }

    public Vector2 GetWheelDistance()
    {
        float leftDistance = leftWheel.GetDeltaDistance();
        float rightDistance = rightWheel.GetDeltaDistance();
        return new Vector2(leftDistance, rightDistance);
    }

    public void MoveWheelchair(Vector2 wheelDistance)
    {
        float leftDistance = wheelDistance.x * moveMultiplier;
        float rightDistance = wheelDistance.y * moveMultiplier;

        // Average distance of both wheels
        float distance = (leftDistance + rightDistance) * 0.5f * speed;

        // Differential rotation
        float rotation = (rightDistance - leftDistance) / wheelSpacing;
        float rotationDegrees = rotation * Mathf.Rad2Deg;

        // Delta rotation
        Quaternion deltaRotation = Quaternion.Euler(0f, rotationDegrees, 0f);

        // Midpoint heading
        Quaternion midpointRotation = rb.rotation * Quaternion.Euler(0f, rotationDegrees * 0.5f, 0f);

        // World-space displacement
        Vector3 movement = midpointRotation * Vector3.forward * distance;

        // Apply Rigidbody movement
        transform.position += movement;
        transform.rotation *= deltaRotation;

        // rb.MovePosition(rb.position + movement);
        // rb.MoveRotation(rb.rotation * deltaRotation);
    }
}