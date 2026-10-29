
using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkCharacterController))]
public class VehicleCharacterMotor : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private NetworkCharacterController ncc;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundStickSpeed = -2f;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (ncc == null) ncc = GetComponent<NetworkCharacterController>();
    }

    public void Move(Vector3 displacement, Quaternion rotation, float dt)
    {
        if (dt <= 0f) return;

        Vector3 previousPosition = transform.position;
        Vector3 velocity = ncc.Velocity;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = groundStickSpeed;
        else
            velocity.y += gravity * dt;

        Vector3 totalMovement = displacement + Vector3.up * velocity.y * dt;

        controller.Move(totalMovement);
        transform.rotation = rotation;

        Vector3 actualVelocity = (transform.position - previousPosition) / dt;

        ncc.Velocity = actualVelocity;
        ncc.Grounded = controller.isGrounded;
    }
}
