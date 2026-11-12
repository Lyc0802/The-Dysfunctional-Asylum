using UnityEngine;

public class VRModel : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private GameObject wheelchair;
    [SerializeField] private GameObject motor;
    [SerializeField] private Transform leftWheel;
    [SerializeField] private Transform rightWheel;
    [SerializeField] private Transform handlebar;

    [Header("Wheel")]
    [SerializeField] private float wheelRadius = 0.5f;
    [SerializeField] private bool invertLeftWheel = false;
    [SerializeField] private bool invertRightWheel = false;
    [SerializeField] private float wheelSmoothTime = 0.08f;

    [Header("Motor")]
    [SerializeField] private float handlebarSmoothSpeed = 15f;

    private float _leftTargetAngle;
    private float _rightTargetAngle;
    private float _leftCurrentAngle;
    private float _rightCurrentAngle;
    private float _leftVelocity;
    private float _rightVelocity;

    private Quaternion _leftInitialRotation;
    private Quaternion _rightInitialRotation;
    private Quaternion _targetHandlebarRotation;

    private void Awake()
    {
        _leftInitialRotation = leftWheel.localRotation;
        _rightInitialRotation = rightWheel.localRotation;
        _targetHandlebarRotation = handlebar.localRotation;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        _leftCurrentAngle = Mathf.SmoothDamp(
            _leftCurrentAngle, _leftTargetAngle,
            ref _leftVelocity, wheelSmoothTime);

        _rightCurrentAngle = Mathf.SmoothDamp(
            _rightCurrentAngle, _rightTargetAngle,
            ref _rightVelocity, wheelSmoothTime);

        leftWheel.localRotation = _leftInitialRotation *
            Quaternion.AngleAxis(_leftCurrentAngle, Vector3.up);

        rightWheel.localRotation = _rightInitialRotation *
            Quaternion.AngleAxis(_rightCurrentAngle, Vector3.up);

        float t = 1f - Mathf.Exp(-handlebarSmoothSpeed * dt);

        handlebar.localRotation = Quaternion.Slerp(
            handlebar.localRotation, _targetHandlebarRotation, t);
    }

    public void UpdateWheelchair(Vector2 angles)
    {
        _leftTargetAngle = angles.x;
        _rightTargetAngle = angles.y;

    }

    public void UpdateMotor(Quaternion handlebarLocalRotation)
    {
        _targetHandlebarRotation = handlebarLocalRotation;
    }

    public void SetModel(bool isMotor)
    {
        motor.SetActive(isMotor);
        wheelchair.SetActive(!isMotor);
    }

    public Vector2 DistanceToAngles(Vector2 distance)
    {
        float factor = Mathf.Rad2Deg / wheelRadius;

        return new Vector2(
            distance.x * factor * (invertLeftWheel ? -1f : 1f),
            distance.y * factor * (invertRightWheel ? -1f : 1f)
        );
    }
}