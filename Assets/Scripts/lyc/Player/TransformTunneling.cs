
using UnityEngine;
using Oculus.Interaction;

public class TransformTunneling : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform movementTarget;
    [SerializeField] private TunnelingEffect tunneling;

    [Header("Strength (Output: FOV Degrees)")]
    [SerializeField] private AnimationCurve rotationStrength = AnimationCurve.Linear(0f, 360f, 120f, 45f);
    [SerializeField] private AnimationCurve movementStrength = AnimationCurve.Linear(0f, 360f, 8f, 55f);
    [SerializeField] private AnimationCurve accelerationStrength = AnimationCurve.Linear(0f, 360f, 15f, 55f);

    [Header("Smoothing")]
    [SerializeField] private float velocitySmoothing = 12f;
    [SerializeField] private float fovSmoothing = 10f;

    [Header("Fade Out")]
    [SerializeField] private float fadeOutWait = 0.2f;
    [SerializeField] private float fadeOutTime = 0.2f;
 
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private Vector3 _lastVelocity;
    private Vector3 _smoothedVelocity;
    private float _currentFOV = 360f;
    private float _fadeOutStart;
    private bool _initialized;
    private bool _active;

    private void OnEnable()
    {
        _initialized = false;
        _active = false;
        _currentFOV = 360f;
    }

    private void Start()
    {
        if (tunneling == null || movementTarget == null)
        {
            Debug.LogError("TransformTunneling: Missing references.", this);
            enabled = false;
            return;
        }

        tunneling.UseAimingTarget = false;
        tunneling.UserFOV = 360f;
        tunneling.enabled = false;
    }

    private void LateUpdate()
    {
        if (movementTarget == null || tunneling == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 position = movementTarget.position;
        Quaternion rotation = movementTarget.rotation;

        if (!_initialized)
        {
            _lastPosition = position;
            _lastRotation = rotation;
            _lastVelocity = Vector3.zero;
            _smoothedVelocity = Vector3.zero;
            _initialized = true;
            return;
        }

        Vector3 velocity = (position - _lastPosition) / dt;
        float angularSpeed = Quaternion.Angle(_lastRotation, rotation) / dt;

        float t = 1f - Mathf.Exp(-velocitySmoothing * dt);
        _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, velocity, t);

        float speed = _smoothedVelocity.magnitude;
        float acceleration = (_smoothedVelocity - _lastVelocity).magnitude / dt;

        _lastPosition = position;
        _lastRotation = rotation;
        _lastVelocity = _smoothedVelocity;

        // Evaluate FOV from movement (smaller = stronger tunneling)
        float movementFOV = movementStrength.Evaluate(speed);
        float rotationFOV = rotationStrength.Evaluate(angularSpeed);
        float accelerationFOV = accelerationStrength.Evaluate(acceleration);

        float targetFOV = Mathf.Min(movementFOV, rotationFOV, accelerationFOV);
        targetFOV = Mathf.Clamp(targetFOV, 1f, 360f);

        bool moving = speed > 0.05f || angularSpeed > 1f || acceleration > 0.1f;

        if (moving)
        {
            _active = true;
            _fadeOutStart = Time.time;

            float fovT = 1f - Mathf.Exp(-fovSmoothing * dt);
            _currentFOV = Mathf.Lerp(_currentFOV, targetFOV, fovT);

            tunneling.enabled = true;
            tunneling.UserFOV = _currentFOV;
            tunneling.AlphaStrength = 1f;
        }
        else if (_active)
        {
            float elapsed = Time.time - _fadeOutStart;

            if (elapsed < fadeOutWait) return;

            float alpha = 1f - Mathf.Clamp01((elapsed - fadeOutWait) / Mathf.Max(0.001f, fadeOutTime));
            tunneling.AlphaStrength = alpha;

            if (alpha <= 0f)
            {
                _active = false;
                _currentFOV = 360f;
                tunneling.UserFOV = 360f;
                tunneling.enabled = false;
            }
        }
    }

    public void ResetTracking()
    {
        _initialized = false;
        _active = false;
        _currentFOV = 360f;

        if (tunneling == null) return;

        tunneling.UserFOV = 360f;
        tunneling.enabled = false;
    }

    private void OnDisable()
    {
        ResetTracking();
    }
}
