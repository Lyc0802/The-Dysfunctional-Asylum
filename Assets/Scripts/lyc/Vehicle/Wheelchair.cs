using UnityEngine;
using Oculus.Interaction.Locomotion;

public class Wheelchair : MonoBehaviour
{
    [Header("Wheels")]
    [SerializeField] private WheelchairWheel leftWheel;
    [SerializeField] private WheelchairWheel rightWheel;

    [Header("Locomotion")]
    [SerializeField] private FirstPersonLocomotor locomotor;

    [Header("Wheelchair Settings")]
    [SerializeField] private float wheelSpacing = 0.8f;
    [SerializeField] private float moveMultiplier = 1f;

    private void Update()
    {
        float leftDistance = leftWheel.GetDeltaDistance();
        float rightDistance = rightWheel.GetDeltaDistance();

        leftDistance *= moveMultiplier;
        rightDistance *= moveMultiplier;

        // Average distance of both wheels
        float distance =
            (leftDistance + rightDistance) * 0.5f;

        // Differential rotation
        float rotation =
            (leftDistance - rightDistance) / wheelSpacing;

        // Delta rotation
        Quaternion deltaRotation =
            Quaternion.Euler(
                0f,
                rotation * Mathf.Rad2Deg,
                0f
            );

        // Midpoint heading
        Quaternion midpointRotation =
            transform.rotation *
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
        var locomotionEvent = new LocomotionEvent(
            0,
            new Pose(movement, deltaRotation),
            LocomotionEvent.TranslationType.Relative,
            LocomotionEvent.RotationType.Relative
        );

        locomotor.HandleLocomotionEvent(locomotionEvent);
    }
}